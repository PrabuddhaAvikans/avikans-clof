using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Finance.Application.Abstractions;
using Finance.Application.Common;
using Finance.Application.CreditNotes;
using Finance.Domain.Common;
using Finance.Domain.CreditNotes;
using Finance.Domain.Invoices;
using Finance.Domain.Sequences;
using Microsoft.EntityFrameworkCore;

namespace Finance.Application.Services;

public sealed class CreditNoteService : ICreditNoteService
{
    private readonly IRepository<CreditNote, Guid> _creditNotes;
    private readonly IRepository<Invoice, Guid> _invoices;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public CreditNoteService(
        IRepository<CreditNote, Guid> creditNotes,
        IRepository<Invoice, Guid> invoices,
        IRepository<DocumentSequence, Guid> sequences,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _creditNotes = creditNotes;
        _invoices = invoices;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<CreditNoteDto>> ListAsync(
        CreditNoteListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _creditNotes.Query()
            .AsNoTracking()
            .Include(x => x.LineItems)
            .Include(x => x.Applications)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (query.CustomerId.HasValue)
            items = items.Where(x => x.CustomerId == query.CustomerId);
        if (query.InvoiceId.HasValue)
            items = items.Where(x => x.InvoiceId == query.InvoiceId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.CreditNoteNumber.Contains(search)
                || x.CustomerName.Contains(search)
                || x.CustomerEmail.Contains(search)
                || (x.InvoiceNumber != null && x.InvoiceNumber.Contains(search)));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<CreditNoteDto>.Create(
            pageItems.Select(FinanceMappers.MapCreditNote).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<CreditNoteDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadAsync(id, cancellationToken, asNoTracking: true);
        return entity is null ? null : FinanceMappers.MapCreditNote(entity);
    }

    public async Task<CreditNoteDto> CreateAsync(
        CreateCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var totals = ComputeTotals(command.LineItems);
            var number = await DocumentNumberGenerator.NextCreditNoteNumberAsync(_sequences, ct);

            var creditNote = CreditNote.Create(
                number,
                command.CustomerId,
                command.CustomerName.Trim(),
                command.CustomerEmail.Trim(),
                command.InvoiceId,
                command.InvoiceNumber,
                command.SalesOrderId,
                command.SalesOrderNumber,
                command.Reason,
                totals.Subtotal,
                totals.TaxAmount,
                totals.TotalAmount,
                string.IsNullOrWhiteSpace(command.Currency) ? FinanceDefaults.Currency : command.Currency.Trim(),
                command.Notes,
                string.IsNullOrWhiteSpace(command.CreatedBy) ? FinanceDefaults.SystemActor : command.CreatedBy.Trim(),
                string.IsNullOrWhiteSpace(command.CreatedByName) ? FinanceDefaults.SystemActorName : command.CreatedByName.Trim());

            ReplaceLines(creditNote, command.LineItems);
            await _creditNotes.AddAsync(creditNote, ct);
            return FinanceMappers.MapCreditNote(creditNote);
        }, cancellationToken);
    }

    public async Task<CreditNoteDto> UpdateAsync(
        UpdateCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var creditNote = await LoadAsync(command.Id, ct)
                ?? throw new NotFoundException($"Credit note '{command.Id}' was not found.");

            if (!CreditNoteStatuses.IsEditable(creditNote.Status))
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(creditNote.Status), "Only draft credit notes can be updated.", ValidationErrorCodes.InvalidState),
                ]);
            }

            var lineItems = command.LineItems;
            var totals = lineItems is null
                ? (creditNote.Subtotal, creditNote.TaxAmount, creditNote.TotalAmount)
                : ComputeTotals(lineItems);

            creditNote.UpdateDraft(
                command.Reason,
                command.InvoiceId,
                command.InvoiceNumber,
                command.SalesOrderId,
                command.SalesOrderNumber,
                totals.Item1,
                totals.Item2,
                totals.Item3,
                command.Currency,
                command.Notes);

            if (lineItems is not null)
                ReplaceLines(creditNote, lineItems);

            return FinanceMappers.MapCreditNote(creditNote);
        }, cancellationToken);
    }

    public async Task<CreditNoteDto> IssueAsync(
        IssueCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var creditNote = await LoadAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{command.Id}' was not found.");

        try
        {
            creditNote.Issue(
                string.IsNullOrWhiteSpace(command.IssuedBy) ? FinanceDefaults.SystemActor : command.IssuedBy.Trim(),
                string.IsNullOrWhiteSpace(command.IssuedByName) ? FinanceDefaults.SystemActorName : command.IssuedByName.Trim());
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(creditNote.Status), ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FinanceMappers.MapCreditNote(creditNote);
    }

    public async Task<CreditNoteDto> VoidAsync(
        VoidCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var creditNote = await LoadAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Credit note '{command.Id}' was not found.");

        try
        {
            creditNote.Void();
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(creditNote.Status), ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return FinanceMappers.MapCreditNote(creditNote);
    }

    public async Task<CreditNoteDto> ApplyToInvoiceAsync(
        ApplyCreditNoteCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var creditNote = await LoadAsync(command.Id, ct)
                ?? throw new NotFoundException($"Credit note '{command.Id}' was not found.");

            var invoice = await _invoices.Query()
                .Include(x => x.LineItems)
                .FirstOrDefaultAsync(x => x.Id == command.InvoiceId, ct)
                ?? throw new NotFoundException($"Invoice '{command.InvoiceId}' was not found.");

            if (invoice.CustomerId != creditNote.CustomerId)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.InvoiceId), "Invoice customer must match credit note customer.", ValidationErrorCodes.Conflict),
                ]);
            }

            try
            {
                creditNote.ApplyToInvoice(
                    invoice.Id,
                    invoice.InvoiceNumber,
                    command.Amount,
                    command.Note ?? string.Empty,
                    string.IsNullOrWhiteSpace(command.AppliedBy) ? FinanceDefaults.SystemActor : command.AppliedBy.Trim(),
                    string.IsNullOrWhiteSpace(command.AppliedByName) ? FinanceDefaults.SystemActorName : command.AppliedByName.Trim());

                invoice.ApplyCredit(command.Amount);
            }
            catch (InvalidOperationException ex)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(nameof(command.Amount), ex.Message, ValidationErrorCodes.InvalidState),
                ]);
            }

            return FinanceMappers.MapCreditNote(creditNote);
        }, cancellationToken);
    }

    private async Task<CreditNote?> LoadAsync(Guid id, CancellationToken cancellationToken, bool asNoTracking = false)
    {
        var query = _creditNotes.Query()
            .Include(x => x.LineItems)
            .Include(x => x.Applications)
            .AsQueryable();
        if (asNoTracking)
            query = query.AsNoTracking();
        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) ComputeTotals(
        IReadOnlyList<CreditNoteLineItemInputDto> lineItems) =>
        FinanceMappers.ComputeTotals(lineItems.Select(l => (l.Quantity, l.UnitPrice, l.TaxPercent)));

    private static void ReplaceLines(CreditNote creditNote, IReadOnlyList<CreditNoteLineItemInputDto> lineItems)
    {
        creditNote.LineItems.Clear();
        var sortOrder = 0;
        foreach (var item in lineItems)
        {
            var lineTotal = item.LineTotal
                ?? FinanceMappers.ComputeLineTotal(item.Quantity, item.UnitPrice, item.TaxPercent);
            creditNote.LineItems.Add(CreditNoteLine.Create(
                creditNote.Id,
                item.ProductId,
                item.ProductSku,
                item.ProductName.Trim(),
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.TaxPercent,
                lineTotal,
                sortOrder++));
        }
    }
}
