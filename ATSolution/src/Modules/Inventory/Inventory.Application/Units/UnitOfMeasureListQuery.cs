using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Units;

public sealed class UnitOfMeasureListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}
