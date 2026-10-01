using System.Text.Json;
namespace Manufacturing.Application.Jobs;

public sealed record MaterialRequirementDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemSku,
    string InventoryItemName,
    decimal RequiredQuantity,
    decimal ReservedQuantity,
    decimal IssuedQuantity,
    string Unit,
    string Status,
    Guid? IssuedFromInventoryItemId,
    Guid? IssuedStockMovementId,
    decimal? IssuedUnitCost);
