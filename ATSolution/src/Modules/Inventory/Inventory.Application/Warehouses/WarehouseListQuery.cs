using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Warehouses;

public sealed class WarehouseListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}
