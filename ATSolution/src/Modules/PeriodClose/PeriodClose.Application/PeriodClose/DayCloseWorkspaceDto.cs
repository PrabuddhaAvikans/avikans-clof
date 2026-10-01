namespace PeriodClose.Application.PeriodClose;

public sealed record DayCloseWorkspaceDto(
    BusinessPeriodDto Period,
    DailyClosingSummaryDto? Summary,
    IReadOnlyList<DayCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionDailySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryDailySnapshotDto> InventorySnapshots,
    IReadOnlyList<WorkerSessionCheckpointDto> SessionCheckpoints,
    IReadOnlyList<EmployeeDayWorkSummaryDto> EmployeeDaySummaries,
    IReadOnlyList<PeriodAuditLogDto> AuditLog,
    bool CanClose,
    bool CanReopen,
    int ActiveSessionCount,
    string WorkerSessionRule,
    int RequiredDailyWorkMinutes,
    bool AllowIncompleteEmployeeHoursException,
    int IncompleteEmployeeCount,
    int OvertimeEmployeeCount,
    int TotalOvertimeMinutes,
    bool RequireOvertimeApproval);
