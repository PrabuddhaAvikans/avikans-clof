using System.Text.Json;
namespace Inventory.Application.Items;

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
