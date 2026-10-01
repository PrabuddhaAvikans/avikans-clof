using System.Text.Json;
namespace Catalog.Application.Products;

public sealed record BomAlternativeDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    string Unit,
    decimal UnitCost,
    bool IsApproved,
    string? Notes);
