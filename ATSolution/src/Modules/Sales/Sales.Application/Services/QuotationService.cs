using System.Text.Json;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Sales.Application.Abstractions;
using Sales.Application.Common;
using Sales.Application.Quotations;
using Sales.Application.SalesOrders;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using Sales.Domain.Sequences;

namespace Sales.Application.Services;

public sealed class QuotationService : IQuotationService
{
    private readonly IRepository<Quotation, Guid> _quotations;
    private readonly IRepository<QuotationLine, Guid> _quotationLines;
    private readonly IRepository<QuotationContact, Guid> _quotationContacts;
    private readonly IRepository<Customer, Guid> _customers;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<CostingRequest, Guid> _costingRequests;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public QuotationService(
        IRepository<Quotation, Guid> quotations,
        IRepository<QuotationLine, Guid> quotationLines,
        IRepository<QuotationContact, Guid> quotationContacts,
        IRepository<Customer, Guid> customers,
        IRepository<Product, Guid> products,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<CostingRequest, Guid> costingRequests,
        IRepository<DocumentSequence, Guid> sequences,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _quotations = quotations;
        _quotationLines = quotationLines;
        _quotationContacts = quotationContacts;
        _customers = customers;
        _products = products;
        _salesOrders = salesOrders;
        _costingRequests = costingRequests;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<QuotationDto>> ListAsync(
        QuotationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _quotations.Query()
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Contacts)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (query.CustomerId.HasValue)
            items = items.Where(x => x.CustomerId == query.CustomerId);
        if (!string.IsNullOrWhiteSpace(query.Priority))
            items = items.Where(x => x.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.Number.Contains(search)
                || x.CustomerName.Contains(search)
                || x.CustomerEmail.Contains(search));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<QuotationDto>.Create(pageItems.Select(SalesMappers.MapQuotation).ToList(), totalCount, page, pageSize);
    }

    public async Task<QuotationDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadQuotationAsync(id, cancellationToken, asNoTracking: true);
        return entity is null ? null : SalesMappers.MapQuotation(entity);
    }

    public async Task<QuotationDto> CreateAsync(
        CreateQuotationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var customer = await _customers.Query()
                .FirstOrDefaultAsync(c => c.Id == command.CustomerId, ct)
                ?? throw new NotFoundException($"Customer '{command.CustomerId}' was not found.");

            var (billing, shipping) = ResolveAddresses(customer);
            var number = await DocumentNumberGenerator.NextAsync(_sequences, DocumentSequenceTypes.Quotation, ct);
            var isDraft = string.Equals(command.SaveMode, "draft", StringComparison.OrdinalIgnoreCase)
                || command.Status == QuotationStatuses.Draft;
            var status = isDraft ? QuotationStatuses.Draft : QuotationStatuses.ReadyToSend;
            var createdBy = command.CreatedBy ?? "system";
            var createdByName = command.CreatedByName ?? "System";
            var discount = command.DiscountAmount ?? 0m;
            var totals = SalesTotals.Compute(
                command.LineItems.Select(l => (l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxPercent)),
                discount);

            var revision = new QuotationRevisionDto(
                Guid.NewGuid(),
                1,
                "v1",
                true,
                isDraft,
                totals.TotalAmount,
                "LKR",
                isDraft ? "Initial draft" : "Initial version",
                DateTimeOffset.UtcNow,
                createdBy,
                createdByName);

            var quotation = Quotation.Create(
                number,
                customer.Id,
                customer.Name,
                customer.Email,
                status,
                command.Priority,
                command.ValidUntil,
                command.Notes,
                command.TermsAndConditions,
                discount,
                totals.Subtotal,
                totals.TaxAmount,
                totals.TotalAmount,
                "LKR",
                PaymentStatuses.Unpaid,
                JsonColumn.Serialize(billing),
                shipping is null ? null : JsonColumn.Serialize(shipping),
                JsonColumn.Serialize(command.Attachments ?? Array.Empty<AttachmentDto>()),
                JsonColumn.Serialize(new[] { revision }),
                command.WorkflowSnapshot.HasValue
                    ? command.WorkflowSnapshot.Value.GetRawText()
                    : JsonColumn.Serialize(new { capturedAt = DateTimeOffset.UtcNow, version = 1 }),
                createdBy,
                createdByName);

            var sort = 0;
            foreach (var line in command.LineItems)
            {
                quotation.Lines.Add(QuotationLine.Create(
                    quotation.Id,
                    line.ProductId,
                    line.ProductSku,
                    line.ProductName,
                    line.ProductVersionId,
                    line.ProductVersionLabel,
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    SalesTotals.ComputeLineTotal(line.Quantity, line.UnitPrice, line.DiscountPercent, line.TaxPercent),
                    line.IsCustomized ?? line.Customization.HasValue,
                    line.RequiresManufacturing ?? false,
                    line.Customization?.GetRawText(),
                    sort++));
            }

            quotation.Contacts.Add(QuotationContact.Create(
                quotation.Id,
                "comment",
                isDraft ? "Quotation draft created" : "Quotation saved",
                null,
                null,
                createdBy,
                createdByName));

            await _quotations.AddAsync(quotation, ct);
            return SalesMappers.MapQuotation(quotation);
        }, cancellationToken);
    }

    public async Task<QuotationDto> UpdateAsync(
        UpdateQuotationCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var quotation = await LoadQuotationAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Quotation '{command.Id}' was not found.");

        if (quotation.Status is QuotationStatuses.Converted or QuotationStatuses.Rejected)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", "Converted or rejected quotations cannot be updated.", "INVALID_STATE"),
            ]);
        }

        if (command.LineItems is not null)
        {
            _quotationLines.RemoveRange(quotation.Lines);
            quotation.Lines.Clear();
            var sort = 0;
            foreach (var line in command.LineItems)
            {
                quotation.Lines.Add(QuotationLine.Create(
                    quotation.Id,
                    line.ProductId,
                    line.ProductSku,
                    line.ProductName,
                    line.ProductVersionId,
                    line.ProductVersionLabel,
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    SalesTotals.ComputeLineTotal(line.Quantity, line.UnitPrice, line.DiscountPercent, line.TaxPercent),
                    line.IsCustomized ?? line.Customization.HasValue,
                    line.RequiresManufacturing ?? false,
                    line.Customization?.GetRawText(),
                    sort++));
            }
        }

        var discount = command.DiscountAmount ?? quotation.DiscountAmount;
        var totals = SalesTotals.Compute(
            quotation.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxPercent)),
            discount);

        var status = quotation.Status;
        if (string.Equals(command.SaveMode, "save", StringComparison.OrdinalIgnoreCase)
            && quotation.Status == QuotationStatuses.Draft)
        {
            status = QuotationStatuses.ReadyToSend;
        }
        else if (command.Status is not null)
        {
            status = command.Status;
        }

        quotation.UpdateHeader(
            command.Priority,
            command.ValidUntil,
            command.Notes,
            command.TermsAndConditions,
            discount,
            totals.Subtotal,
            totals.TaxAmount,
            totals.TotalAmount,
            status,
            command.Attachments is null ? null : JsonColumn.Serialize(command.Attachments),
            null);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SalesMappers.MapQuotation(quotation);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quotation = await LoadQuotationAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Quotation '{id}' was not found.");

        if (quotation.Status is not QuotationStatuses.Draft and not QuotationStatuses.ReadyToSend)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", "Only draft or ready-to-send quotations can be deleted.", "INVALID_STATE"),
            ]);
        }

        _quotations.Remove(quotation);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<QuotationDto> SendAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var quotation = await _quotations.Query()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Quotation '{id}' was not found.");

        var pending = quotation.Lines.FirstOrDefault(line =>
            line.IsCustomized
            && !string.IsNullOrWhiteSpace(line.CustomizationJson)
            && (line.CustomizationJson.Contains($"\"status\":\"{CustomizationStatuses.Draft}\"")
                || line.CustomizationJson.Contains($"\"status\":\"{CustomizationStatuses.PendingApproval}\"")));

        if (pending is not null)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "lineItems",
                    "Customized lines must be estimated and approved before sending the quotation.",
                    "INVALID_STATE"),
            ]);
        }

        quotation.MarkSent();
        await _quotationContacts.AddAsync(
            QuotationContact.Create(
                quotation.Id,
                "email",
                "Quotation sent to customer",
                $"Email sent to {quotation.CustomerEmail}",
                "Delivered",
                quotation.CreatedBy,
                quotation.CreatedByName),
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        var fresh = await LoadQuotationAsync(id, cancellationToken, asNoTracking: true);
        return SalesMappers.MapQuotation(fresh!);
    }

    public async Task<SalesOrderDto> ConvertToSalesOrderAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var quotation = await LoadQuotationAsync(id, ct)
                ?? throw new NotFoundException($"Quotation '{id}' was not found.");

            if (quotation.Status is not QuotationStatuses.Accepted and not QuotationStatuses.Sent)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError("status", "Quotation must be accepted or sent before conversion.", "INVALID_STATE"),
                ]);
            }

            foreach (var line in quotation.Lines)
            {
                if (!string.IsNullOrWhiteSpace(line.CustomizationJson))
                {
                    line.LockCustomization(LockCustomizationJson(line.CustomizationJson));
                }
            }

            var number = await DocumentNumberGenerator.NextAsync(_sequences, DocumentSequenceTypes.SalesOrder, ct);
            var order = SalesOrder.Create(
                number,
                quotation.CustomerId,
                quotation.CustomerName,
                quotation.CustomerEmail,
                quotation.Id,
                quotation.Number,
                quotation.Priority,
                quotation.DiscountAmount,
                quotation.Subtotal,
                quotation.TaxAmount,
                quotation.TotalAmount,
                quotation.Currency,
                quotation.BillingAddressJson,
                quotation.ShippingAddressJson,
                null,
                quotation.Notes,
                quotation.WorkflowSnapshotJson,
                quotation.CreatedBy,
                quotation.CreatedByName);

            var sort = 0;
            foreach (var line in quotation.Lines.OrderBy(l => l.SortOrder))
            {
                order.Lines.Add(SalesOrderLine.Create(
                    order.Id,
                    line.ProductId,
                    line.ProductSku,
                    line.ProductName,
                    line.ProductVersionId,
                    line.ProductVersionLabel,
                    line.Description,
                    line.Quantity,
                    line.UnitPrice,
                    line.DiscountPercent,
                    line.TaxPercent,
                    line.LineTotal,
                    line.IsCustomized,
                    line.RequiresManufacturing,
                    line.CustomizationJson,
                    sort++));
            }

            await _salesOrders.AddAsync(order, ct);

            var costingNumber = await DocumentNumberGenerator.NextAsync(_sequences, DocumentSequenceTypes.CostingRequest, ct);
            var built = CostingBuilder.BuildFromSalesOrder(order, costingNumber, null);
            var costing = CostingRequest.Create(
                built.RequestNumber,
                order.Id,
                order.Number,
                order.QuotationId,
                order.QuotationNumber,
                order.CustomerName,
                $"SO {order.Number}",
                built.TotalEstimate,
                built.ProposedPrice,
                built.MarginPercent,
                order.Currency,
                "Net 30",
                built.LineItemsJson,
                built.CoatingItemsJson,
                built.EstimationMaterialsJson,
                built.EstimationProductLinesJson,
                built.RequesterJson,
                built.ApprovalLevelsJson,
                built.HistoryJson,
                built.Status,
                built.CoatingStatus,
                built.ConfigSnapshotJson);
            await _costingRequests.AddAsync(costing, ct);
            order.SetCostingRequest(costing.Id);
            quotation.MarkConverted(order.Id);

            return SalesMappers.MapSalesOrder(order);
        }, cancellationToken);
    }

    public async Task<QuotationDto> AddContactEntryAsync(
        AddQuotationContactCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var quotation = await LoadQuotationAsync(command.QuotationId, cancellationToken)
            ?? throw new NotFoundException($"Quotation '{command.QuotationId}' was not found.");

        if (quotation.Status is QuotationStatuses.Accepted or QuotationStatuses.Converted or QuotationStatuses.Rejected)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", "Contact history can only be logged until the quotation is approved.", "INVALID_STATE"),
            ]);
        }

        quotation.Contacts.Add(QuotationContact.Create(
            quotation.Id,
            command.Type,
            command.Summary,
            command.Detail,
            command.Outcome,
            command.ContactedBy ?? "system",
            command.ContactedByName ?? "System"));
        quotation.Touch();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SalesMappers.MapQuotation(quotation);
    }

    public async Task<QuotationDto> ApproveLineCustomizationAsync(
        Guid quotationId,
        Guid lineItemId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var quotation = await LoadQuotationAsync(quotationId, cancellationToken)
            ?? throw new NotFoundException($"Quotation '{quotationId}' was not found.");

        var line = quotation.Lines.FirstOrDefault(l => l.Id == lineItemId)
            ?? throw new NotFoundException($"Quotation line '{lineItemId}' was not found.");

        if (string.IsNullOrWhiteSpace(line.CustomizationJson))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("lineItemId", "Line item has no customization to approve.", "INVALID_STATE"),
            ]);
        }

        using var doc = JsonDocument.Parse(line.CustomizationJson);
        var root = doc.RootElement.Clone();
        var updated = ApproveCustomizationElement(root, notes, quotation.CreatedBy, quotation.CreatedByName);
        line.UpdateCustomization(updated, true);
        quotation.Touch();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SalesMappers.MapQuotation(quotation);
    }

    public async Task<PromoteCustomizationResultDto> PromoteCustomizationToProductVersionAsync(
        Guid quotationId,
        Guid lineItemId,
        string? revisionNotes,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var quotation = await LoadQuotationAsync(quotationId, ct)
                ?? throw new NotFoundException($"Quotation '{quotationId}' was not found.");

            var line = quotation.Lines.FirstOrDefault(l => l.Id == lineItemId)
                ?? throw new NotFoundException($"Quotation line '{lineItemId}' was not found.");

            if (!line.IsCustomized || string.IsNullOrWhiteSpace(line.CustomizationJson) || !line.ProductId.HasValue)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError("lineItemId", "Only customized quotation lines can be promoted to a product version.", "INVALID_STATE"),
                ]);
            }

            var product = await _products.Query()
                .Include(p => p.Versions)
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .FirstOrDefaultAsync(p => p.Id == line.ProductId.Value, ct)
                ?? throw new NotFoundException($"Product '{line.ProductId}' was not found.");

            using var doc = JsonDocument.Parse(line.CustomizationJson);
            var customization = doc.RootElement;
            var nextNumber = product.Versions.Count == 0 ? 1 : product.Versions.Max(v => v.VersionNumber) + 1;
            var notes = revisionNotes
                ?? $"Promoted from quotation {quotation.Number} customization";

            var specs = customization.TryGetProperty("customizedSpecifications", out var specsEl)
                ? specsEl.GetRawText()
                : "{}";
            var bom = customization.TryGetProperty("customizedBom", out var bomEl)
                ? bomEl.GetRawText()
                : "[]";
            var operations = customization.TryGetProperty("customizedOperations", out var opsEl)
                ? opsEl.GetRawText()
                : "[]";
            var costBreakdown = customization.TryGetProperty("estimation", out var est)
                && est.TryGetProperty("costBreakdown", out var cb)
                    ? cb.GetRawText()
                    : "{}";
            var sellingPrice = customization.TryGetProperty("estimation", out var est2)
                && est2.TryGetProperty("sellingPrice", out var sp)
                && sp.TryGetDecimal(out var selling)
                    ? selling
                    : 0m;
            var costPrice = customization.TryGetProperty("estimation", out var est3)
                && est3.TryGetProperty("costPrice", out var cp)
                && cp.TryGetDecimal(out var cost)
                    ? cost
                    : 0m;
            var margin = sellingPrice > 0
                ? Math.Round((sellingPrice - costPrice) / sellingPrice * 100m, 2, MidpointRounding.AwayFromZero)
                : 0m;

            var version = ProductVersion.Create(
                product.Id,
                nextNumber,
                ProductVersionStatuses.Draft,
                specs,
                bom,
                operations,
                "[]",
                "[]",
                costBreakdown,
                "[]",
                sellingPrice,
                costPrice,
                margin,
                0,
                1,
                null,
                notes);

            product.Versions.Add(version);
            product.SetCurrentVersion(version.Id);

            var timestamp = DateTimeOffset.UtcNow;
            var historyEntry = new
            {
                id = Guid.NewGuid(),
                at = timestamp,
                by = quotation.CreatedBy,
                byName = quotation.CreatedByName,
                action = "promoted_to_version",
                detail = $"Created {version.Label} on master product (explicit reuse)",
            };

            var mutable = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(line.CustomizationJson, JsonColumn.Options)
                ?? new Dictionary<string, JsonElement>();
            mutable["promotedProductVersionId"] = JsonSerializer.SerializeToElement(version.Id.ToString(), JsonColumn.Options);
            mutable["updatedAt"] = JsonSerializer.SerializeToElement(timestamp, JsonColumn.Options);
            var history = mutable.TryGetValue("history", out var histEl)
                ? JsonSerializer.Deserialize<List<object>>(histEl.GetRawText(), JsonColumn.Options) ?? new List<object>()
                : new List<object>();
            history.Insert(0, historyEntry);
            mutable["history"] = JsonSerializer.SerializeToElement(history, JsonColumn.Options);
            line.UpdateCustomization(JsonColumn.Serialize(mutable), true);
            quotation.Touch();

            var productDto = new
            {
                id = product.Id,
                sku = product.Sku,
                name = product.Name,
                description = product.Description ?? "",
                categoryId = product.CategoryId,
                categoryName = product.Category?.Name ?? "",
                brandId = product.BrandId,
                brandName = product.Brand?.Name,
                productType = product.ProductType,
                customerId = product.CustomerId,
                customerName = product.CustomerName,
                projectId = product.ProjectId,
                projectName = product.ProjectName,
                currency = product.Currency,
                status = product.Status,
                currentVersionId = product.CurrentVersionId ?? version.Id,
                versions = product.Versions.OrderBy(v => v.VersionNumber).Select(v => new
                {
                    id = v.Id,
                    productId = v.ProductId,
                    versionNumber = v.VersionNumber,
                    label = v.Label,
                    status = v.Status,
                    isLocked = v.IsLocked,
                    specifications = JsonColumn.ParseElement(v.SpecificationsJson),
                    bom = JsonColumn.ParseElement(v.BomJson),
                    operations = JsonColumn.ParseElement(v.OperationsJson),
                    attributes = JsonColumn.ParseElement(v.AttributesJson),
                    images = JsonColumn.ParseElement(v.ImagesJson),
                    costBreakdown = JsonColumn.ParseElement(v.CostBreakdownJson),
                    basePrice = v.SellingPrice,
                    costPrice = v.CostPrice,
                    leadTimeDays = v.LeadTimeDays,
                    minOrderQuantity = v.MinOrderQuantity,
                    tags = JsonColumn.Deserialize(v.TagsJson, Array.Empty<string>()),
                    revisionNotes = v.RevisionNotes,
                    createdAt = v.CreatedOnUtc,
                    updatedAt = v.ModifiedOnUtc,
                    releasedAt = v.ReleasedAtUtc,
                    releasedBy = v.ReleasedBy,
                    approvedAt = v.ApprovedAtUtc,
                }),
                basePrice = version.SellingPrice,
                costPrice = version.CostPrice,
                createdAt = product.CreatedOnUtc,
                updatedAt = product.ModifiedOnUtc,
            };

            return new PromoteCustomizationResultDto(SalesMappers.MapQuotation(quotation), productDto);
        }, cancellationToken);
    }

    private async Task<Quotation?> LoadQuotationAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        var query = _quotations.Query()
            .Include(x => x.Lines)
            .Include(x => x.Contacts)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    private static (AddressDto Billing, AddressDto? Shipping) ResolveAddresses(Customer customer)
    {
        var billingAddresses = JsonColumn.Deserialize(customer.BillingAddressesJson, Array.Empty<AddressDto>());
        var billing = billingAddresses.Length > 0
            ? billingAddresses[Math.Clamp(customer.ActiveBillingAddressIndex, 0, billingAddresses.Length - 1)]
            : new AddressDto("", null, "", "", "", "");

        AddressDto? shipping = null;
        if (!customer.DeliverySameAsBilling && !string.IsNullOrWhiteSpace(customer.ShippingAddressesJson))
        {
            var shippingAddresses = JsonColumn.Deserialize(customer.ShippingAddressesJson, Array.Empty<AddressDto>());
            if (shippingAddresses.Length > 0)
            {
                var index = customer.ActiveShippingAddressIndex ?? 0;
                shipping = shippingAddresses[Math.Clamp(index, 0, shippingAddresses.Length - 1)];
            }
        }

        return (billing, shipping);
    }

    private static string LockCustomizationJson(string json)
    {
        var mutable = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json, JsonColumn.Options)
            ?? new Dictionary<string, JsonElement>();
        mutable["isLocked"] = JsonSerializer.SerializeToElement(true, JsonColumn.Options);
        return JsonColumn.Serialize(mutable);
    }

    private static string ApproveCustomizationElement(
        JsonElement root,
        string? notes,
        string by,
        string byName)
    {
        var mutable = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(root.GetRawText(), JsonColumn.Options)
            ?? new Dictionary<string, JsonElement>();
        mutable["status"] = JsonSerializer.SerializeToElement(CustomizationStatuses.Approved, JsonColumn.Options);
        var approval = mutable.TryGetValue("approval", out var approvalEl)
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(approvalEl.GetRawText(), JsonColumn.Options)
              ?? new Dictionary<string, JsonElement>()
            : new Dictionary<string, JsonElement>();
        approval["status"] = JsonSerializer.SerializeToElement(CustomizationStatuses.Approved, JsonColumn.Options);
        approval["decidedAt"] = JsonSerializer.SerializeToElement(DateTimeOffset.UtcNow, JsonColumn.Options);
        approval["decidedBy"] = JsonSerializer.SerializeToElement(by, JsonColumn.Options);
        approval["decidedByName"] = JsonSerializer.SerializeToElement(byName, JsonColumn.Options);
        if (notes is not null)
        {
            approval["notes"] = JsonSerializer.SerializeToElement(notes, JsonColumn.Options);
        }

        mutable["approval"] = JsonSerializer.SerializeToElement(approval, JsonColumn.Options);
        mutable["updatedAt"] = JsonSerializer.SerializeToElement(DateTimeOffset.UtcNow, JsonColumn.Options);
        return JsonColumn.Serialize(mutable);
    }
}
