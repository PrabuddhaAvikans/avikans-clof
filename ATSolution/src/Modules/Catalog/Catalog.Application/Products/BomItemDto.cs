using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record BomItemDto(
    Guid Id,
    int Sequence,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    decimal Quantity,
    string Unit,
    decimal WastePercent,
    decimal RequiredQuantity,
    decimal UnitCost,
    decimal LineCost,
    bool IsRequired,
    string? Notes,
    IReadOnlyList<BomAlternativeDto> Alternatives);
