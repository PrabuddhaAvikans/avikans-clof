using ATSolution.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Abstractions;
using Reporting.Application.Dashboard;

namespace Reporting.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Reporting.Dashboard)]
[ApiController]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet(ApiRoutes.Reporting.Summary)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetSummaryAsync(cancellationToken));
    }

    [HttpGet(ApiRoutes.Reporting.OrderFlow)]
    public async Task<ActionResult<OrderFlowOverviewDto>> GetOrderFlow(CancellationToken cancellationToken)
    {
        return Ok(await _dashboardService.GetOrderFlowAsync(cancellationToken));
    }
}
