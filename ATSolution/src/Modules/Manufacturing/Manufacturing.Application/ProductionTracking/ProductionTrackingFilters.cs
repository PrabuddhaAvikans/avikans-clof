using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.ProductionTracking;

public sealed class ProductionTrackingFilters : PaginatedRequest
{
    public string? Line { get; set; }
    public Guid? SupervisorId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public bool? DelayedOnly { get; set; }
}
