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

    [HttpGet("settings")]
    public async Task<ActionResult<PeriodCloseSettingsDto>> GetSettings(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetSettingsAsync(branchId, cancellationToken));
    }

    [HttpPut("settings")]
    public async Task<ActionResult<PeriodCloseSettingsDto>> UpdateSettings(
        [FromBody] UpdatePeriodCloseSettingsCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.UpdateSettingsAsync(command, cancellationToken));
    }

    [HttpGet("days")]
    public async Task<ActionResult<PaginatedResponse<BusinessPeriodDto>>> ListDays(
        [FromQuery] BusinessPeriodListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListDayPeriodsAsync(query, cancellationToken));
    }

    [HttpGet("months")]
    public async Task<ActionResult<PaginatedResponse<MonthlyPeriodDto>>> ListMonths(
        [FromQuery] MonthlyPeriodListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListMonthlyPeriodsAsync(query, cancellationToken));
    }

    [HttpGet("days/current")]
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

    [HttpGet("days/{id:guid}")]
    public async Task<ActionResult<DayCloseWorkspaceDto>> GetDayWorkspace(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetDayWorkspaceAsync(id, cancellationToken));
    }

    [HttpGet("months/current")]
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

    [HttpGet("months/{id:guid}")]
    public async Task<ActionResult<MonthlyCloseWorkspaceDto>> GetMonthWorkspace(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetMonthWorkspaceAsync(id, cancellationToken));
    }

    [HttpPost("days/{id:guid}/validate")]
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

    [HttpPost("days/{id:guid}/close")]
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

    [HttpPost("days/{id:guid}/reopen")]
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

    [HttpPost("months/{id:guid}/validate")]
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

    [HttpPost("months/{id:guid}/close")]
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

    [HttpPost("months/{id:guid}/reopen")]
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

    [HttpGet("days/{id:guid}/audit")]
    public async Task<ActionResult<IReadOnlyList<PeriodAuditLogDto>>> ListDayAudit(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListDayAuditAsync(id, cancellationToken));
    }

    [HttpGet("months/{id:guid}/audit")]
    public async Task<ActionResult<IReadOnlyList<PeriodAuditLogDto>>> ListMonthAudit(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListMonthAuditAsync(id, cancellationToken));
    }

    [HttpGet("days/{id:guid}/summary")]
    public async Task<ActionResult<DailyClosingSummaryDto?>> GetDailySummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetDailySummaryAsync(id, cancellationToken));
    }

    [HttpGet("months/{id:guid}/summary")]
    public async Task<ActionResult<MonthlyClosingSummaryDto?>> GetMonthlySummary(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.GetMonthlySummaryAsync(id, cancellationToken));
    }

    [HttpGet("days/{id:guid}/production-snapshots")]
    public async Task<ActionResult<IReadOnlyList<ProductionDailySnapshotDto>>> ListProductionDailySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListProductionDailySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet("months/{id:guid}/production-snapshots")]
    public async Task<ActionResult<IReadOnlyList<ProductionMonthlySnapshotDto>>> ListProductionMonthlySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListProductionMonthlySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet("days/{id:guid}/inventory-snapshots")]
    public async Task<ActionResult<IReadOnlyList<InventoryDailySnapshotDto>>> ListInventoryDailySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListInventoryDailySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet("months/{id:guid}/inventory-snapshots")]
    public async Task<ActionResult<IReadOnlyList<InventoryMonthlySnapshotDto>>> ListInventoryMonthlySnapshots(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListInventoryMonthlySnapshotsAsync(id, cancellationToken));
    }

    [HttpGet("days/{id:guid}/session-checkpoints")]
    public async Task<ActionResult<IReadOnlyList<WorkerSessionCheckpointDto>>> ListSessionCheckpoints(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListSessionCheckpointsAsync(id, cancellationToken));
    }

    [HttpPost("adjustments")]
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

    [HttpGet("adjustments")]
    public async Task<ActionResult<IReadOnlyList<PeriodAdjustmentDto>>> ListAdjustments(
        [FromQuery] string? branchId,
        CancellationToken cancellationToken)
    {
        return Ok(await _periodCloseService.ListAdjustmentsAsync(branchId, cancellationToken));
    }

    [HttpPost("assert-writable")]
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
