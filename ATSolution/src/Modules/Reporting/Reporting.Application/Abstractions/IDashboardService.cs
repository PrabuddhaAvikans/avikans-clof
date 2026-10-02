using Reporting.Application.Dashboard;

namespace Reporting.Application.Abstractions;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<OrderFlowOverviewDto> GetOrderFlowAsync(CancellationToken cancellationToken = default);
}
