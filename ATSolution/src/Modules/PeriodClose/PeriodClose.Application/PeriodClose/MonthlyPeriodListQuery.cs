using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed class MonthlyPeriodListQuery : PaginatedRequest
{
    public string? BranchId { get; set; }
    public string? Status { get; set; }
    public int? Year { get; set; }
}
