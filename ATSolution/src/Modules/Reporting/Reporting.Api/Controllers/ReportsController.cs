using ATSolution.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Reporting.Application.Abstractions;
using Reporting.Application.Reports;

namespace Reporting.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Reporting.Reports)]
[ApiController]
public sealed class ReportsController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsController(IReportService reportService)
    {
        _reportService = reportService;
    }

    [HttpGet(ApiRoutes.Reporting.ByReportId)]
    public async Task<ActionResult<ReportDatasetDto>> GetReport(string reportId, CancellationToken cancellationToken)
    {
        return Ok(await _reportService.GetReportAsync(reportId, cancellationToken));
    }
}
