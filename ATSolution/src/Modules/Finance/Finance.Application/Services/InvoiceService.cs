using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.Invoices;
using Finance.Domain.Common;
using Finance.Domain.Invoices;
using Finance.Domain.Sequences;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Services;

public sealed class InvoiceService : IInvoiceService
{
    private readonly IRepository<Invoice, Guid> _invoices;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;
    private readonly IBusinessPeriodGuard _periodGuard;

    public InvoiceService(
        IRepository<Invoice, Guid> invoices,
        IRepository<DocumentSequence, Guid> sequences,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IBusinessPeriodGuard periodGuard)
    {
        _invoices = invoices;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _periodGuard = periodGuard;
    }

    public async Task<PaginatedResponse<InvoiceDto>> ListAsync(
        InvoiceListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _invoices.Query()
            .AsNoTracking()
            .Include(x => x.LineItems)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (query.CustomerId.HasValue)
            items = items.Where(x => x.CustomerId == query.CustomerId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.InvoiceNumber.Contains(search)
                || x.CustomerName.Contains(search)
                || x.CustomerEmail.Contains(search)
                || (x.SalesOrderNumber != null && x.SalesOrderNumber.Contains(search)));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<InvoiceDto>.Create(
            pageItems.Select(FinanceMappers.MapInvoice).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<InvoiceDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(id, cancellationToken, asNoTracking: true);
        return entity is null ? null : FinanceMappers.MapInvoice(entity);
    }

    public async Task<InvoiceDto> CreateAsync(
        CreateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await _periodGuard.EnsureWritableAsync(command.IssueDate == default ? DateTimeOffset.UtcNow : command.IssueDate, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var totals = ComputeTotals(command.LineItems);
            var number = await DocumentNumberGenerator.NextInvoiceNumberAsync(_sequences, ct);

            var invoice = Invoice.Create(
                number,
                command.CustomerId,
                command.CustomerName.Trim(),
                command.CustomerEmail.Trim(),
                command.SalesOrderId,
                command.SalesOrderNumber,
                command.IssueDate,
                command.DueDate,
                totals.Subtotal,
                totals.TaxAmount,
                totals.TotalAmount,
                string.IsNullOrWhiteSpace(command.Currency) ? FinanceDefaults.Currency : command.Currency.Trim(),
                command.Notes,
                string.IsNullOrWhiteSpace(command.CreatedBy) ? FinanceDefaults.SystemActor : command.CreatedBy.Trim(),
                string.IsNullOrWhiteSpace(command.CreatedByName) ? FinanceDefaults.SystemActorName : command.CreatedByName.Trim());

            ReplaceLines(invoice, command.LineItems);
            await _invoices.AddAsync(invoice, ct);
            return FinanceMappers.MapInvoice(invoice);
        }, cancellationToken);
    }

    public async Task<InvoiceDto> UpdateAsync(
        UpdateInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var invoice = await LoadAsync(command.Id, ct)
                ?? throw new NotFoundException($"Invoice '{command.Id}' was not found.");

            if (!InvoiceStatuses.IsEditable(invoice.Status))
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(invoice.Status), "Only draft invoices can be updated.", ValidationErrorCodes.InvalidState),
                ]);
            }

            var lineItems = command.LineItems;
            var totals = lineItems is null
                ? (invoice.Subtotal, invoice.TaxAmount, invoice.TotalAmount)
                : ComputeTotals(lineItems);

            invoice.UpdateDraft(
                command.IssueDate,
                command.DueDate,
                totals.Item1,
                totals.Item2,
                totals.Item3,
                command.Currency,
                command.Notes);

            if (lineItems is not null)
                ReplaceLines(invoice, lineItems);

            return FinanceMappers.MapInvoice(invoice);
        }, cancellationToken);
    }

    public async Task<InvoiceDto> IssueAsync(
        IssueInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var invoice = await LoadAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{command.Id}' was not found.");

        try
        {
            invoice.Issue();
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(invoice.Status), ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FinanceMappers.MapInvoice(invoice);
    }

    public async Task<InvoiceDto> VoidAsync(
        VoidInvoiceCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var invoice = await LoadAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{command.Id}' was not found.");

        try
        {
            invoice.Void();
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(invoice.Status), ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FinanceMappers.MapInvoice(invoice);
    }

    public async Task<InvoiceDto> RecordPaymentAsync(
        RecordInvoicePaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);

        var invoice = await LoadAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Invoice '{command.Id}' was not found.");

        try
        {
            invoice.RecordPayment(command.Amount);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Amount), ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FinanceMappers.MapInvoice(invoice);
    }

    private async Task<Invoice?> LoadAsync(Guid id, CancellationToken cancellationToken, bool asNoTracking = false)
    {
        var query = _invoices.Query().Include(x => x.LineItems).AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) ComputeTotals(
        IReadOnlyList<InvoiceLineItemInputDto> lineItems) =>
        FinanceMappers.ComputeTotals(lineItems.Select(l => (l.Quantity, l.UnitPrice, l.TaxPercent)));

    private static void ReplaceLines(Invoice invoice, IReadOnlyList<InvoiceLineItemInputDto> lineItems)
    {
        invoice.LineItems.Clear();
        var sortOrder = 0;
        foreach (var item in lineItems)
        {
            var lineTotal = item.LineTotal
                ?? FinanceMappers.ComputeLineTotal(item.Quantity, item.UnitPrice, item.TaxPercent);
            invoice.LineItems.Add(InvoiceLine.Create(
                invoice.Id,
                item.ProductId,
                item.ProductSku.Trim(),
                item.ProductName.Trim(),
                item.Quantity,
                item.UnitPrice,
                item.TaxPercent,
                lineTotal,
                sortOrder++));
        }
    }
}
