using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Warehouses;

public sealed class WarehouseListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}

public sealed record WarehouseDto(
    Guid Id,
    string Code,
    string Name,
    string? Address,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateWarehouseCommand(string Code, string Name, string? Address, string Status);
public sealed record UpdateWarehouseCommand(Guid Id, string? Code = null, string? Name = null, string? Address = null, string? Status = null);
