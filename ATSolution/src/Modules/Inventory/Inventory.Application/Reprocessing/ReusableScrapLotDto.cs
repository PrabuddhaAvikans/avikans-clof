namespace Inventory.Application.Reprocessing;

public sealed record ReusableScrapLotDto(
    Guid Id,
    string Sku,
    string Name,
    decimal QuantityAvailable,
    string Unit,
    decimal CostPrice);
