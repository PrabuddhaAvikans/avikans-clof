using System.Text.Json;
namespace Sales.Application.Costing;

public sealed record EstimationMaterialInputDto(
    Guid? Id,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    decimal Quantity,
    string Unit,
    decimal WastePercent,
    decimal UnitCost,
    bool IsRequired,
    Guid? AlternativeItemId,
    string? AlternativeItemName,
    string? Notes,
    Guid? SalesOrderLineItemId,
    string? SourceType,
    string? SourceProductName,
    string? ProductVersionLabel);
