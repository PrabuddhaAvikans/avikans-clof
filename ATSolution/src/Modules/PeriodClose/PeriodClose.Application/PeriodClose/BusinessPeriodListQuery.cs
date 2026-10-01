using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed class BusinessPeriodListQuery : PaginatedRequest
{
    public string? BranchId { get; set; }
    public string? Status { get; set; }
    public string? From { get; set; }
    public string? To { get; set; }
}
