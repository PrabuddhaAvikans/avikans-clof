using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PeriodClose.Application.Abstractions;
using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Api.Controllers;

[Authorize]
[Route(ApiRoutes.PeriodClose.Base)]
[ApiController]
public sealed class PeriodCloseController : ControllerBase
{
    private readonly IPeriodCloseService _periodCloseService;

    public PeriodCloseController(IPeriodCloseService periodCloseService)
    {
        _periodCloseService = periodCloseService;
    }

    [HttpGet(ApiRoutes.PeriodClose.Settings)]
    public async Task<ActionResult<PeriodCloseSettingsDto>> GetSettings(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetSettingsAsync(branchId, cancellationToken));
    }

    [HttpPut(ApiRoutes.PeriodClose.Settings)]
    public async Task<ActionResult<PeriodCloseSettingsDto>> UpdateSettings(
        [FromBody] UpdatePeriodCloseSettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.UpdateSettingsAsync(command, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.Days)]
    public async Task<ActionResult<PaginatedResponse<BusinessPeriodDto>>> ListDays(
        [FromQuery] BusinessPeriodListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListDayPeriodsAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.Months)]
    public async Task<ActionResult<PaginatedResponse<MonthlyPeriodDto>>> ListMonths(
        [FromQuery] MonthlyPeriodListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListMonthlyPeriodsAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.CurrentDay)]
    public async Task<ActionResult<DayCloseWorkspaceDto>> GetCurrentDay(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetCurrentDayAsync(
            branchId,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DayById)]
    public async Task<ActionResult<DayCloseWorkspaceDto>> GetDayWorkspace(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetDayWorkspaceAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.CurrentMonth)]
    public async Task<ActionResult<MonthlyCloseWorkspaceDto>> GetCurrentMonth(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetCurrentMonthAsync(
            branchId,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.MonthById)]
    public async Task<ActionResult<MonthlyCloseWorkspaceDto>> GetMonthWorkspace(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetMonthWorkspaceAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.ValidateDay)]
    public async Task<ActionResult<DayCloseWorkspaceDto>> ValidateDay(
        Guid id,
        [FromBody] CloseDayOptions? options,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.RunDayValidationAsync(
            id,
            options,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.CloseDay)]
    public async Task<ActionResult<CloseDayResultDto>> CloseDay(
        Guid id,
        [FromBody] CloseDayOptions? options,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.CloseDayAsync(
            id,
            options,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.ReopenDay)]
    public async Task<ActionResult<BusinessPeriodDto>> ReopenDay(
        Guid id,
        [FromBody] ReopenPeriodCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ReopenDayAsync(
            id,
            command,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.ValidateMonth)]
    public async Task<ActionResult<MonthlyCloseWorkspaceDto>> ValidateMonth(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.RunMonthValidationAsync(
            id,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.CloseMonth)]
    public async Task<ActionResult<CloseMonthResultDto>> CloseMonth(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.CloseMonthAsync(
            id,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.ReopenMonth)]
    public async Task<ActionResult<MonthlyPeriodDto>> ReopenMonth(
        Guid id,
        [FromBody] ReopenPeriodCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ReopenMonthAsync(
            id,
            command,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DayAudit)]
    public async Task<ActionResult<IReadOnlyList<PeriodAuditLogDto>>> ListDayAudit(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListDayAuditAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.MonthAudit)]
    public async Task<ActionResult<IReadOnlyList<PeriodAuditLogDto>>> ListMonthAudit(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListMonthAuditAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DaySummary)]
    public async Task<ActionResult<DailyClosingSummaryDto?>> GetDailySummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetDailySummaryAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.MonthSummary)]
    public async Task<ActionResult<MonthlyClosingSummaryDto?>> GetMonthlySummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetMonthlySummaryAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DayProductionSnapshots)]
    public async Task<ActionResult<IReadOnlyList<ProductionDailySnapshotDto>>> ListProductionDailySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListProductionDailySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.MonthProductionSnapshots)]
    public async Task<ActionResult<IReadOnlyList<ProductionMonthlySnapshotDto>>> ListProductionMonthlySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListProductionMonthlySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DayInventorySnapshots)]
    public async Task<ActionResult<IReadOnlyList<InventoryDailySnapshotDto>>> ListInventoryDailySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListInventoryDailySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.MonthInventorySnapshots)]
    public async Task<ActionResult<IReadOnlyList<InventoryMonthlySnapshotDto>>> ListInventoryMonthlySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListInventoryMonthlySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.DaySessionCheckpoints)]
    public async Task<ActionResult<IReadOnlyList<WorkerSessionCheckpointDto>>> ListSessionCheckpoints(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListSessionCheckpointsAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.Adjustments)]
    public async Task<ActionResult<PeriodAdjustmentDto>> CreateAdjustment(
        [FromBody] CreatePeriodAdjustmentCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.CreateAdjustmentAsync(
            command,
            GetActorUserId(),
            GetActorUserName(),
            cancellationToken));
    }

    [HttpGet(ApiRoutes.PeriodClose.Adjustments)]
    public async Task<ActionResult<IReadOnlyList<PeriodAdjustmentDto>>> ListAdjustments(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListAdjustmentsAsync(branchId, cancellationToken));
    }

    [HttpPost(ApiRoutes.PeriodClose.AssertWritable)]
    public async Task<ActionResult> AssertWritable(
        [FromBody] AssertWritableCommand command,
        CancellationToken cancellationToken)
    {
        await _periodCloseService.AssertWritableAsync(command.BranchId, command.BusinessDate, cancellationToken);
        return Ok(ApiResponse.Succeeded("Period is writable."));
    }

    private string? GetActorUserId() =>
        Request.Headers.TryGetValue("X-User-Id", out var value) ? value.ToString() : User.FindFirst("sub")?.Value;

    private string? GetActorUserName() =>
        Request.Headers.TryGetValue("X-User-Name", out var value) ? value.ToString() : User.Identity?.Name;
}
