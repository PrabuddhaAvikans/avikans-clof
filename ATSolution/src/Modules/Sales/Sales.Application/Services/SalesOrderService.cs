using System.Text.Json;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Inventory.Domain.Common;
using Inventory.Domain.Movements;
using Sales.Application.Abstractions;
using Sales.Application.Common;
using Sales.Application.Quotations;
using Sales.Application.SalesOrders;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;

namespace Sales.Application.Services;

public sealed class SalesOrderService : ISalesOrderService
{
    private readonly ISalesOrderRepository _salesOrders;
    private readonly IRepository<Product, Guid> _products;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;
    private readonly IBusinessPeriodGuard _periodGuard;

    public SalesOrderService(
        ISalesOrderRepository salesOrders,
        IRepository<Product, Guid> products,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IBusinessPeriodGuard periodGuard)
    {
        _salesOrders = salesOrders;
        _products = products;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _periodGuard = periodGuard;
    }

    public async Task<PaginatedResponse<SalesOrderDto>> ListAsync(
        SalesOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var (pageItems, totalCount) = await _salesOrders.SearchAsync(query, page, pageSize, cancellationToken);
        return PaginatedResponse<SalesOrderDto>.Create(pageItems.Select(SalesMappers.MapSalesOrder).ToList(), totalCount, page, pageSize);
    }

    public async Task<SalesOrderDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await LoadOrderAsync(id, cancellationToken, asNoTracking: true);
        return entity is null ? null : SalesMappers.MapSalesOrder(entity);
    }

    public async Task<SalesOrderDto> CreateAsync(
        CreateSalesOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var customer = await _salesOrders.FindCustomerAsync(command.CustomerId, ct)
                ?? throw new NotFoundException(string.Format(SalesMessages.CustomerNotFound, command.CustomerId));

            Quotation? quotation = null;
            if (command.QuotationId.HasValue)
            {
                quotation = await _salesOrders.FindQuotationWithLinesAsync(command.QuotationId.Value, ct);
            }

            var (billing, shipping) = ResolveAddresses(customer);
            var number = await _salesOrders.NextNumberAsync(DocumentSequenceTypes.SalesOrder, ct);
            var discount = command.DiscountAmount ?? 0m;
            var totals = SalesTotals.Compute(
                command.LineItems.Select(l => (l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxPercent)),
                discount);

            var order = SalesOrder.Create(
                number,
                customer.Id,
                customer.Name,
                customer.Email,
                command.QuotationId ?? quotation?.Id,
                command.QuotationNumber ?? quotation?.Number,
                command.Priority,
                discount,
                totals.Subtotal,
                totals.TaxAmount,
                totals.TotalAmount,
                SalesDefaults.Currency,
                JsonColumn.Serialize(billing),
                shipping is null ? null : JsonColumn.Serialize(shipping),
                command.RequestedDeliveryDate,
                command.Notes,
                command.WorkflowSnapshot is not null
                    ? command.WorkflowSnapshot.ToJsonString()
                    : JsonColumn.Serialize(new { capturedAt = DateTimeOffset.UtcNow, version = 1 }),
                command.CreatedBy ?? SalesDefaults.SystemActor,
                command.CreatedByName ?? SalesDefaults.SystemActorName);

            var sort = 0;
            foreach (var line in command.LineItems)
            {
                var customizationJson = line.CustomizationJson;
                if (!string.IsNullOrWhiteSpace(customizationJson))
                {
                    customizationJson = LockCustomizationJson(customizationJson);
                }

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
                    SalesTotals.ComputeLineTotal(line.Quantity, line.UnitPrice, line.DiscountPercent, line.TaxPercent),
                    line.IsCustomized ?? line.Customization is not null,
                    line.RequiresManufacturing ?? false,
                    customizationJson,
                    sort++));
            }

            await _salesOrders.AddOrderAsync(order, ct);

            var costingNumber = await _salesOrders.NextNumberAsync(DocumentSequenceTypes.CostingRequest, ct);
            var resolve = await ProductCostCatalog.LoadAsync(_products, order.Lines.Select(line => line.ProductId), ct);
            var built = CostingBuilder.BuildFromSalesOrder(order, costingNumber, null, resolve);
            var costing = CostingRequest.Create(
                built.RequestNumber,
                order.Id,
                order.Number,
                order.QuotationId,
                order.QuotationNumber,
                order.CustomerName,
                string.Format(SalesDefaults.SalesOrderTitleFormat, order.Number),
                built.TotalEstimate,
                built.ProposedPrice,
                built.MarginPercent,
                order.Currency,
                SalesDefaults.PaymentTerms,
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
            await _salesOrders.AddCostingAsync(costing, ct);
            order.SetCostingRequest(costing.Id);

            if (quotation is not null)
            {
                quotation.MarkConverted(order.Id);
            }

            return SalesMappers.MapSalesOrder(order);
        }, cancellationToken);
    }

    public async Task<SalesOrderDto> UpdateAsync(
        UpdateSalesOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await LoadOrderAsync(command.Id, ct)
                ?? throw new NotFoundException(string.Format(SalesMessages.SalesOrderNotFound, command.Id));

            if (!SalesOrderStatuses.IsConfirmable(order.Status))
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(SalesValidationFields.Status, SalesMessages.OnlyOpenOrdersCanBeUpdated, ValidationErrorCodes.InvalidState),
                ]);
            }

            if (command.LineItems is not null)
            {
                _salesOrders.RemoveLines(order.Lines);
                order.Lines.Clear();
                var sort = 0;
                foreach (var line in command.LineItems)
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
                        SalesTotals.ComputeLineTotal(line.Quantity, line.UnitPrice, line.DiscountPercent, line.TaxPercent),
                        line.IsCustomized ?? line.Customization is not null,
                        line.RequiresManufacturing ?? false,
                        line.CustomizationJson,
                        sort++));
                }
            }

            var discount = command.DiscountAmount ?? order.DiscountAmount;
            var totals = SalesTotals.Compute(
                order.Lines.Select(l => (l.Quantity, l.UnitPrice, l.DiscountPercent, l.TaxPercent)),
                discount);

            order.Update(
                command.Priority,
                command.RequestedDeliveryDate,
                command.Notes,
                discount,
                totals.Subtotal,
                totals.TaxAmount,
                totals.TotalAmount);

            if (command.LineItems is not null && SalesOrderStatuses.IsConfirmable(order.Status))
            {
                var existing = await _salesOrders.FindCostingByOrderAsync(order.Id, ct);
                if (existing is null
                    || (existing.Status is not CostingRequestStatuses.Approved
                        and not CostingRequestStatuses.Rejected))
                {
                    var number = existing?.Number
                        ?? await _salesOrders.NextNumberAsync(DocumentSequenceTypes.CostingRequest, ct);
                    var resolve = await ProductCostCatalog.LoadAsync(_products, order.Lines.Select(line => line.ProductId), ct);
                    var built = CostingBuilder.BuildFromSalesOrder(order, number, existing, resolve);
                    if (existing is null)
                    {
                        existing = CostingRequest.Create(
                            built.RequestNumber,
                            order.Id,
                            order.Number,
                            order.QuotationId,
                            order.QuotationNumber,
                            order.CustomerName,
                            string.Format(SalesDefaults.SalesOrderTitleFormat, order.Number),
                            built.TotalEstimate,
                            built.ProposedPrice,
                            built.MarginPercent,
                            order.Currency,
                            SalesDefaults.PaymentTerms,
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
                        await _salesOrders.AddCostingAsync(existing, ct);
                        order.SetCostingRequest(existing.Id);
                    }
                    else
                    {
                        existing.SyncContent(
                            built.TotalEstimate,
                            built.ProposedPrice,
                            built.MarginPercent,
                            built.LineItemsJson,
                            built.CoatingItemsJson,
                            built.EstimationMaterialsJson,
                            built.EstimationProductLinesJson,
                            built.Status,
                            built.CoatingStatus,
                            built.HistoryJson);
                    }
                }
            }

            return SalesMappers.MapSalesOrder(order);
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(id, cancellationToken)
            ?? throw new NotFoundException(string.Format(SalesMessages.SalesOrderNotFound, id));

        if (!SalesOrderStatuses.IsDeletable(order.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(SalesValidationFields.Status, SalesMessages.OnlyDeletableOrders, ValidationErrorCodes.InvalidState),
            ]);
        }

        var costing = await _salesOrders.FindCostingByOrderAsync(id, cancellationToken);
        if (costing is not null)
        {
            _salesOrders.RemoveCosting(costing);
        }

        _salesOrders.RemoveOrder(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<SalesOrderDto> ConfirmAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await LoadOrderAsync(id, ct)
                ?? throw new NotFoundException(string.Format(SalesMessages.SalesOrderNotFound, id));

            if (!SalesOrderStatuses.IsConfirmable(order.Status))
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(SalesValidationFields.Status, SalesMessages.OrderCannotBeConfirmed, ValidationErrorCodes.InvalidState),
                ]);
            }

            var costing = await _salesOrders.FindCostingByOrderAsync(order.Id, ct);

            if (costing is null)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(SalesValidationFields.Costing, SalesMessages.CostingRequiredBeforeConfirm, ValidationErrorCodes.InvalidState),
                ]);
            }

            if (costing.CoatingStatus == CoatingStatuses.Pending)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(
                        SalesValidationFields.CoatingStatus,
                        SalesMessages.EstimationStillPending,
                        ValidationErrorCodes.InvalidState),
                ]);
            }

            if (costing.CoatingStatus is not CoatingStatuses.Submitted and not CoatingStatuses.Skipped)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(SalesValidationFields.CoatingStatus, SalesMessages.CoatingMustBeSubmittedOrSkipped, ValidationErrorCodes.InvalidState),
                ]);
            }

            if (costing.Status != CostingRequestStatuses.Approved)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(SalesValidationFields.Costing, SalesMessages.CostingMustBeApproved, ValidationErrorCodes.InvalidState),
                ]);
            }

            // Snapshot costing/BOM onto lines
            var materials = JsonColumn.ParseElement(costing.EstimationMaterialsJson);
            var productLines = JsonColumn.ParseElement(costing.EstimationProductLinesJson);
            foreach (var line in order.Lines)
            {
                string? bomSnapshot = null;
                string? costSnapshot = null;
                if (materials.HasValue && materials.Value.ValueKind == JsonValueKind.Array)
                {
                    var lineMaterials = materials.Value.EnumerateArray()
                        .Where(m =>
                            m.TryGetProperty(SalesJsonFields.SalesOrderLineItemId, out var sid)
                            && Guid.TryParse(sid.GetString(), out var lineId)
                            && lineId == line.Id)
                        .Select(m => JsonSerializer.Deserialize<object>(m.GetRawText(), JsonColumn.Options)!)
                        .ToList();
                    bomSnapshot = JsonColumn.Serialize(lineMaterials);
                }

                if (productLines.HasValue && productLines.Value.ValueKind == JsonValueKind.Array)
                {
                    var match = productLines.Value.EnumerateArray().FirstOrDefault(p =>
                        p.TryGetProperty(SalesJsonFields.SalesOrderLineItemId, out var sid)
                        && Guid.TryParse(sid.GetString(), out var lineId)
                        && lineId == line.Id);
                    if (match.ValueKind != JsonValueKind.Undefined)
                    {
                        costSnapshot = match.GetRawText();
                    }
                }

                line.ApplySnapshots(bomSnapshot, costSnapshot, line.CustomizationJson);
            }

            order.Confirm(JsonColumn.Serialize(new
            {
                costingRequestId = costing.Id,
                costingNumber = costing.Number,
                estimationMaterials = materials,
                estimationProductLines = productLines,
                snappedAt = DateTimeOffset.UtcNow,
            }));

            await CreateReservationsFromCostingAsync(order, costing, ct);
            return SalesMappers.MapSalesOrder(order);
        }, cancellationToken);
    }

    public async Task<SalesOrderDto> CancelAsync(
        CancelSalesOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var order = await LoadOrderAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(string.Format(SalesMessages.SalesOrderNotFound, command.Id));

        if (!SalesOrderStatuses.IsCancellable(order.Status))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    SalesValidationFields.Status,
                    SalesMessages.OrderCannotBeCancelled,
                    ValidationErrorCodes.InvalidState),
            ]);
        }

        order.Cancel(command.Reason);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SalesMappers.MapSalesOrder(order);
    }

    public async Task<SalesOrderDto> AssignAsync(
        AssignSalesOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var order = await LoadOrderAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException(string.Format(SalesMessages.SalesOrderNotFound, command.Id));

        var user = await _salesOrders.FindUserAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException(string.Format(SalesMessages.UserNotFound, command.UserId));

        order.Assign(user.Id, user.FullName);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return SalesMappers.MapSalesOrder(order);
    }

    private async Task CreateReservationsFromCostingAsync(
        SalesOrder order,
        CostingRequest costing,
        CancellationToken cancellationToken)
    {
        var materials = JsonColumn.ParseElement(costing.EstimationMaterialsJson);
        if (!materials.HasValue || materials.Value.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var mat in materials.Value.EnumerateArray())
        {
            if (!mat.TryGetProperty(SalesJsonFields.InventoryItemId, out var idEl)
                || !Guid.TryParse(idEl.GetString(), out var inventoryItemId)
                || inventoryItemId == Guid.Empty)
            {
                continue;
            }

            var required = mat.TryGetProperty(SalesJsonFields.RequiredQuantity, out var rq) && rq.TryGetDecimal(out var reqQty)
                ? reqQty
                : mat.TryGetProperty(SalesJsonFields.Quantity, out var q) && q.TryGetDecimal(out var qty) ? qty : 0m;

            if (required <= 0) continue;

            var item = await _salesOrders.FindInventoryItemAsync(inventoryItemId, cancellationToken);
            if (item is null) continue;

            try
            {
                item.ApplyReservation(required);
            }
            catch (InvalidOperationException)
            {
                // Soft-fail reservation when stock is insufficient; confirmation still proceeds.
                continue;
            }

            var movement = StockMovement.Create(
                item.Id,
                item.Name,
                item.Sku,
                StockMovementTypes.Reservation,
                required,
                item.Unit,
                SalesDefaults.SalesOrderReferenceType,
                order.Id.ToString(),
                string.Format(SalesDefaults.ReservationNoteFormat, order.Number),
                order.CreatedBy,
                order.CreatedByName,
                JsonColumn.Serialize(new { costingRequestId = costing.Id }));

            await _salesOrders.AddStockMovementAsync(movement, cancellationToken);
        }
    }

    private async Task<SalesOrder?> LoadOrderAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        return await _salesOrders.GetWithLinesAsync(id, asNoTracking, cancellationToken);
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
        mutable[SalesJsonFields.IsLocked] = JsonSerializer.SerializeToElement(true, JsonColumn.Options);
        return JsonColumn.Serialize(mutable);
    }
}
