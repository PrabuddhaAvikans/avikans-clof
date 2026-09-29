using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Units;

public sealed class UnitOfMeasureListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}

public sealed record UnitOfMeasureDto(
    Guid Id,
    string Code,
    string Name,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateUnitOfMeasureCommand(string Code, string Name, string Status);
public sealed record UpdateUnitOfMeasureCommand(Guid Id, string? Code = null, string? Name = null, string? Status = null);
