using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Reprocessing;

public sealed class ReprocessingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? InputScrapLotId { get; set; }
}
