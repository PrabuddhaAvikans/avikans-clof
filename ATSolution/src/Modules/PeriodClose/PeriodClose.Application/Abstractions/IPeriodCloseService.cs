using ATSolution.SharedKernel.Models;
using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public interface IPeriodCloseService
{
    Task<PeriodCloseSettingsDto> GetSettingsAsync(string? branchId = null, CancellationToken cancellationToken = default);
    Task<PeriodCloseSettingsDto> UpdateSettingsAsync(UpdatePeriodCloseSettingsCommand command, CancellationToken cancellationToken = default);

    Task<PaginatedResponse<BusinessPeriodDto>> ListDayPeriodsAsync(BusinessPeriodListQuery query, CancellationToken cancellationToken = default);
    Task<PaginatedResponse<MonthlyPeriodDto>> ListMonthlyPeriodsAsync(MonthlyPeriodListQuery query, CancellationToken cancellationToken = default);

    Task<DayCloseWorkspaceDto> GetCurrentDayAsync(string? branchId = null, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<DayCloseWorkspaceDto> GetDayWorkspaceAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<MonthlyCloseWorkspaceDto> GetCurrentMonthAsync(string? branchId = null, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<MonthlyCloseWorkspaceDto> GetMonthWorkspaceAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<DayCloseWorkspaceDto> RunDayValidationAsync(Guid periodId, CloseDayOptions? options = null, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<CloseDayResultDto> CloseDayAsync(Guid periodId, CloseDayOptions? options = null, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<BusinessPeriodDto> ReopenDayAsync(Guid periodId, ReopenPeriodCommand command, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);

    Task<MonthlyCloseWorkspaceDto> RunMonthValidationAsync(Guid periodId, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<CloseMonthResultDto> CloseMonthAsync(Guid periodId, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<MonthlyPeriodDto> ReopenMonthAsync(Guid periodId, ReopenPeriodCommand command, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PeriodAuditLogDto>> ListDayAuditAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PeriodAuditLogDto>> ListMonthAuditAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<DailyClosingSummaryDto?> GetDailySummaryAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<MonthlyClosingSummaryDto?> GetMonthlySummaryAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionDailySnapshotDto>> ListProductionDailySnapshotsAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductionMonthlySnapshotDto>> ListProductionMonthlySnapshotsAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryDailySnapshotDto>> ListInventoryDailySnapshotsAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryMonthlySnapshotDto>> ListInventoryMonthlySnapshotsAsync(Guid periodId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkerSessionCheckpointDto>> ListSessionCheckpointsAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<PeriodAdjustmentDto> CreateAdjustmentAsync(CreatePeriodAdjustmentCommand command, string? actorUserId = null, string? actorUserName = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PeriodAdjustmentDto>> ListAdjustmentsAsync(string? branchId = null, CancellationToken cancellationToken = default);

    Task AssertWritableAsync(string branchId, string businessDate, CancellationToken cancellationToken = default);
}
