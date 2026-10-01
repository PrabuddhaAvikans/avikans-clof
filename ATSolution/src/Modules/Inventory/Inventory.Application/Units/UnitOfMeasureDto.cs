namespace Inventory.Application.Units;

public sealed record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
