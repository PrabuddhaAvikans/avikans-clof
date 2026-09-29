using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed class InventoryListQuery : PaginatedRequest
{
    public string? Category { get; set; }
    public string? ItemType { get; set; }
    public string? StockStatus { get; set; }
    public string? Status { get; set; }
    public string? Location { get; set; }
    public string? Warehouse { get; set; }
}

public sealed class StockMovementListQuery : PaginatedRequest
{
    public Guid? InventoryItemId { get; set; }
    public string? Type { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceId { get; set; }
}

public sealed record InventoryItemDto(
    Guid Id,
    string Sku,
    string Name,
    string? Description,
    string Category,
    string ItemType,
    string Unit,
    string? Brand,
    string? Supplier,
    string? TaxCode,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityAvailable,
    string Warehouse,
    string Location,
    decimal MinStock,
    decimal MaxStock,
    decimal ReorderLevel,
    decimal ReorderQuantity,
    decimal? BuyingPrice,
    decimal CostPrice,
    decimal UnitCost,
    string PricingMethod,
    decimal MarkupPercent,
    decimal MarkupFixedAmount,
    decimal SellingPrice,
    DateTimeOffset PricingEffectiveDate,
    string StockStatus,
    string Status,
    DateTimeOffset? LastRestockedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record StockMovementTraceDto
{
    public string? SourceInventoryTransactionId { get; init; }
    public string? SourceProductionOrderId { get; init; }
    public string? SourceProductionBatchId { get; init; }
    public string? SourceMaterialLotId { get; init; }
    public string? ReprocessingBatchId { get; init; }
    public string? ParentMaterialTransactionId { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal? CarriedValue { get; init; }
}

public sealed record StockMovementDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string InventoryItemSku,
    string Type,
    decimal Quantity,
    string Unit,
    string? ReferenceType,
    string? ReferenceId,
    string? Notes,
    string PerformedBy,
    string PerformedByName,
    DateTimeOffset PerformedAt,
    StockMovementTraceDto? Trace);

public sealed record InventoryPriceHistoryDto(
    Guid Id,
    Guid InventoryItemId,
    decimal? BuyingPrice,
    decimal CostPrice,
    decimal SellingPrice,
    string PricingMethod,
    decimal MarkupPercent,
    decimal MarkupFixedAmount,
    DateTimeOffset EffectiveDate,
    string ChangedBy,
    string ChangedByName,
    DateTimeOffset CreatedAt);

public sealed record CreateInventoryItemCommand(
    string Sku,
    string Name,
    string? Description,
    string Category,
    string ItemType,
    string Unit,
    string? Brand,
    string? Supplier,
    string? TaxCode,
    decimal QuantityOnHand,
    string Warehouse,
    string Location,
    decimal MinStock,
    decimal MaxStock,
    decimal ReorderLevel,
    decimal ReorderQuantity,
    decimal? BuyingPrice,
    decimal CostPrice,
    string PricingMethod,
    decimal MarkupPercent,
    decimal MarkupFixedAmount,
    decimal SellingPrice,
    DateTimeOffset PricingEffectiveDate,
    string Status);

public sealed record UpdateInventoryItemCommand(
    Guid Id,
    string? Sku = null,
    string? Name = null,
    string? Description = null,
    string? Category = null,
    string? ItemType = null,
    string? Unit = null,
    string? Brand = null,
    string? Supplier = null,
    string? TaxCode = null,
    decimal? QuantityOnHand = null,
    string? Warehouse = null,
    string? Location = null,
    decimal? MinStock = null,
    decimal? MaxStock = null,
    decimal? ReorderLevel = null,
    decimal? ReorderQuantity = null,
    decimal? BuyingPrice = null,
    decimal? CostPrice = null,
    string? PricingMethod = null,
    decimal? MarkupPercent = null,
    decimal? MarkupFixedAmount = null,
    decimal? SellingPrice = null,
    DateTimeOffset? PricingEffectiveDate = null,
    string? Status = null);

public sealed record RecordStockMovementCommand(
    Guid InventoryItemId,
    string Type,
    decimal Quantity,
    string? ReferenceType = null,
    string? ReferenceId = null,
    string? Notes = null,
    JsonElement? Trace = null,
    string? PerformedBy = null,
    string? PerformedByName = null);
