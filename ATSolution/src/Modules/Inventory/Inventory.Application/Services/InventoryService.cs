using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Items;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services;

public sealed class InventoryService : IInventoryService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRepository<InventoryItem, Guid> _items;
    private readonly IRepository<InventoryPriceHistory, Guid> _priceHistory;
    private readonly IRepository<StockMovement, Guid> _movements;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;
    private readonly IBusinessPeriodGuard _periodGuard;

    public InventoryService(
        IRepository<InventoryItem, Guid> items,
        IRepository<InventoryPriceHistory, Guid> priceHistory,
        IRepository<StockMovement, Guid> movements,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IBusinessPeriodGuard periodGuard)
    {
        _items = items;
        _priceHistory = priceHistory;
        _movements = movements;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _periodGuard = periodGuard;
    }

    public async Task<PaginatedResponse<InventoryItemDto>> ListAsync(
        InventoryListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _items.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Category))
            items = items.Where(i => i.Category == query.Category);
        if (!string.IsNullOrWhiteSpace(query.ItemType))
            items = items.Where(i => i.ItemType == query.ItemType);
        if (!string.IsNullOrWhiteSpace(query.StockStatus))
            items = items.Where(i => i.StockStatus == query.StockStatus);
        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(i => i.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Location))
            items = items.Where(i => i.Location == query.Location);
        if (!string.IsNullOrWhiteSpace(query.Warehouse))
            items = items.Where(i => i.Warehouse == query.Warehouse);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(i =>
                i.Name.Contains(search)
                || i.Sku.Contains(search)
                || (i.Description != null && i.Description.Contains(search))
                || i.Category.Contains(search)
                || i.Warehouse.Contains(search)
                || (i.Brand != null && i.Brand.Contains(search)));
        }

        items = items.OrderBy(i => i.Name);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<InventoryItemDto>.Create(pageItems.Select(MapItem).ToList(), totalCount, page, pageSize);
    }

    public async Task<InventoryItemDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _items.Query().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return item is null ? null : MapItem(item);
    }

    public async Task<InventoryItemDto> CreateAsync(
        CreateInventoryItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var skuExists = await _items.Query().AnyAsync(i => i.Sku == command.Sku, cancellationToken);
        if (skuExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Sku), "Inventory SKU already exists.", ValidationErrorCodes.Conflict),
            ]);
        }

        var selling = ResolveSellingPrice(command.CostPrice, command.PricingMethod, command.MarkupPercent, command.MarkupFixedAmount, command.SellingPrice);
        var item = InventoryItem.Create(
            command.Sku,
            command.Name,
            command.Description,
            command.Category,
            command.ItemType,
            command.Unit,
            command.Brand,
            command.Supplier,
            command.TaxCode,
            command.QuantityOnHand,
            command.Warehouse,
            command.Location,
            command.MinStock,
            command.MaxStock,
            command.ReorderLevel,
            command.ReorderQuantity,
            command.BuyingPrice,
            command.CostPrice,
            command.PricingMethod,
            command.MarkupPercent,
            command.MarkupFixedAmount,
            selling,
            command.PricingEffectiveDate,
            command.Status);

        await _items.AddAsync(item, cancellationToken);
        await _priceHistory.AddAsync(InventoryPriceHistory.Create(
            item.Id,
            item.BuyingPrice,
            item.CostPrice,
            item.SellingPrice,
            item.PricingMethod,
            item.MarkupPercent,
            item.MarkupFixedAmount,
            item.PricingEffectiveDate,
            "system",
            "System"));

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task<InventoryItemDto> UpdateAsync(
        UpdateInventoryItemCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var item = await _items.Query()
            .FirstOrDefaultAsync(i => i.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{command.Id}' was not found.");

        var previousCost = item.CostPrice;
        var previousSell = item.SellingPrice;
        var previousMethod = item.PricingMethod;
        var previousMarkupPercent = item.MarkupPercent;
        var previousMarkupFixed = item.MarkupFixedAmount;
        var previousBuying = item.BuyingPrice;

        var nextCost = command.CostPrice ?? item.CostPrice;
        var nextMethod = command.PricingMethod ?? item.PricingMethod;
        var nextMarkupPercent = command.MarkupPercent ?? item.MarkupPercent;
        var nextMarkupFixed = command.MarkupFixedAmount ?? item.MarkupFixedAmount;
        var nextSell = ResolveSellingPrice(
            nextCost,
            nextMethod,
            nextMarkupPercent,
            nextMarkupFixed,
            command.SellingPrice ?? item.SellingPrice);

        item.UpdateDetails(
            command.Name,
            command.Description,
            command.Category,
            command.ItemType,
            command.Unit,
            command.Brand,
            command.Supplier,
            command.TaxCode,
            command.Warehouse,
            command.Location,
            command.MinStock,
            command.MaxStock,
            command.ReorderLevel,
            command.ReorderQuantity,
            command.BuyingPrice,
            nextCost,
            nextMethod,
            nextMarkupPercent,
            nextMarkupFixed,
            nextSell,
            command.PricingEffectiveDate,
            command.Status,
            command.QuantityOnHand);

        var pricingChanged =
            previousCost != item.CostPrice
            || previousSell != item.SellingPrice
            || previousMethod != item.PricingMethod
            || previousMarkupPercent != item.MarkupPercent
            || previousMarkupFixed != item.MarkupFixedAmount
            || previousBuying != item.BuyingPrice;

        if (pricingChanged)
        {
            await _priceHistory.AddAsync(InventoryPriceHistory.Create(
                item.Id,
                item.BuyingPrice,
                item.CostPrice,
                item.SellingPrice,
                item.PricingMethod,
                item.MarkupPercent,
                item.MarkupFixedAmount,
                item.PricingEffectiveDate,
                "system",
                "System"));
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapItem(item);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _items.Query()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{id}' was not found.");

        item.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetLowStockAsync(CancellationToken cancellationToken = default)
    {
        var items = await _items.Query()
            .AsNoTracking()
            .Where(i => i.Status == EntityStatuses.Active
                && (i.StockStatus == StockStatuses.LowStock || i.StockStatus == StockStatuses.OutOfStock))
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        return items.Select(MapItem).ToList();
    }

    public async Task<PaginatedResponse<StockMovementDto>> ListMovementsAsync(
        StockMovementListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var movements = _movements.Query().AsNoTracking().AsQueryable();

        if (query.InventoryItemId.HasValue)
            movements = movements.Where(m => m.InventoryItemId == query.InventoryItemId);
        if (!string.IsNullOrWhiteSpace(query.Type))
            movements = movements.Where(m => m.Type == query.Type);
        if (!string.IsNullOrWhiteSpace(query.ReferenceType))
            movements = movements.Where(m => m.ReferenceType == query.ReferenceType);
        if (!string.IsNullOrWhiteSpace(query.ReferenceId))
            movements = movements.Where(m => m.ReferenceId == query.ReferenceId);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            movements = movements.Where(m =>
                m.InventoryItemName.Contains(search)
                || m.InventoryItemSku.Contains(search)
                || (m.Notes != null && m.Notes.Contains(search)));
        }

        movements = movements.OrderByDescending(m => m.PerformedAtUtc);
        var totalCount = await movements.CountAsync(cancellationToken);
        var pageItems = await movements.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<StockMovementDto>.Create(pageItems.Select(MapMovement).ToList(), totalCount, page, pageSize);
    }

    public async Task<StockMovementDto> RecordMovementAsync(
        RecordStockMovementCommand command,
        CancellationToken cancellationToken = default)
    {
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var item = await _items.Query()
                .FirstOrDefaultAsync(i => i.Id == command.InventoryItemId, ct)
                ?? throw new NotFoundException($"Inventory item '{command.InventoryItemId}' was not found.");

            var type = command.Type.Trim().ToLowerInvariant();
            try
            {
                switch (type)
                {
                    case StockMovementTypes.Receipt:
                        item.ApplyReceipt(command.Quantity);
                        break;
                    case StockMovementTypes.Issue:
                        item.ApplyIssue(command.Quantity);
                        break;
                    case StockMovementTypes.Reservation:
                        item.ApplyReservation(command.Quantity);
                        break;
                    case StockMovementTypes.Release:
                        item.ApplyRelease(command.Quantity);
                        break;
                    case StockMovementTypes.Adjustment:
                        item.ApplyAdjustment(command.Quantity);
                        break;
                    case StockMovementTypes.Transfer:
                        item.ApplyTransfer(command.Quantity);
                        break;
                    default:
                        throw new ApplicationValidationException(
                        [
                            new ValidationError(nameof(command.Type), $"Unsupported movement type '{command.Type}'.", "Validation"),
                        ]);
                }
            }
            catch (InvalidOperationException ex)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError("quantity", ex.Message, "INSUFFICIENT_STOCK"),
                ]);
            }

            var absQty = Math.Abs(command.Quantity);
            var recordedQty = type == StockMovementTypes.Adjustment ? command.Quantity : absQty;
            var traceJson = command.Trace.HasValue ? command.Trace.Value.GetRawText() : null;

            var movement = StockMovement.Create(
                item.Id,
                item.Name,
                item.Sku,
                type,
                recordedQty,
                item.Unit,
                command.ReferenceType,
                command.ReferenceId,
                command.Notes,
                command.PerformedBy ?? "system",
                command.PerformedByName ?? "System",
                traceJson);

            await _movements.AddAsync(movement, cancellationToken);
            return MapMovement(movement);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryPriceHistoryDto>> GetPriceHistoryAsync(
        Guid inventoryItemId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _items.Query().AnyAsync(i => i.Id == inventoryItemId, cancellationToken);
        if (!exists)
        {
            throw new NotFoundException($"Inventory item '{inventoryItemId}' was not found.");
        }

        var history = await _priceHistory.Query()
            .AsNoTracking()
            .Where(h => h.InventoryItemId == inventoryItemId)
            .OrderByDescending(h => h.EffectiveDateUtc)
            .ToListAsync(cancellationToken);

        return history.Select(h => new InventoryPriceHistoryDto(
            h.Id,
            h.InventoryItemId,
            h.BuyingPrice,
            h.CostPrice,
            h.SellingPrice,
            h.PricingMethod,
            h.MarkupPercent,
            h.MarkupFixedAmount,
            h.EffectiveDateUtc,
            h.ChangedBy,
            h.ChangedByName,
            h.CreatedOnUtc)).ToList();
    }

    public async Task<InventoryItemDto?> FindBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        var item = await _items.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Sku == sku, cancellationToken);
        return item is null ? null : MapItem(item);
    }

    private static InventoryItemDto MapItem(InventoryItem item) =>
        new(
            item.Id,
            item.Sku,
            item.Name,
            item.Description,
            item.Category,
            item.ItemType,
            item.Unit,
            item.Brand,
            item.Supplier,
            item.TaxCode,
            item.QuantityOnHand,
            item.QuantityReserved,
            item.QuantityAvailable,
            item.Warehouse,
            item.Location,
            item.MinStock,
            item.MaxStock,
            item.ReorderLevel,
            item.ReorderQuantity,
            item.BuyingPrice,
            item.CostPrice,
            item.CostPrice,
            item.PricingMethod,
            item.MarkupPercent,
            item.MarkupFixedAmount,
            item.SellingPrice,
            item.PricingEffectiveDate,
            item.StockStatus,
            item.Status,
            item.LastRestockedAtUtc,
            item.CreatedOnUtc,
            item.ModifiedOnUtc);

    private static StockMovementDto MapMovement(StockMovement movement)
    {
        StockMovementTraceDto? trace = null;
        if (!string.IsNullOrWhiteSpace(movement.TraceJson))
        {
            try
            {
                trace = JsonSerializer.Deserialize<StockMovementTraceDto>(movement.TraceJson, JsonOptions);
            }
            catch
            {
                trace = null;
            }
        }

        return new StockMovementDto(
            movement.Id,
            movement.InventoryItemId,
            movement.InventoryItemName,
            movement.InventoryItemSku,
            movement.Type,
            movement.Quantity,
            movement.Unit,
            movement.ReferenceType,
            movement.ReferenceId,
            movement.Notes,
            movement.PerformedBy,
            movement.PerformedByName,
            movement.PerformedAtUtc,
            trace);
    }

    private static decimal ResolveSellingPrice(
        decimal costPrice,
        string pricingMethod,
        decimal markupPercent,
        decimal markupFixedAmount,
        decimal sellingPrice) =>
        pricingMethod switch
        {
            "percentage_markup" => Math.Round(costPrice * (1 + markupPercent / 100m), 4),
            "fixed_markup" => Math.Round(costPrice + markupFixedAmount, 4),
            _ => sellingPrice,
        };
}
