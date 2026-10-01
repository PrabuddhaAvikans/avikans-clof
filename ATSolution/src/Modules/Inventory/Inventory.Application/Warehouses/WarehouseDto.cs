namespace Inventory.Application.Warehouses;

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
