namespace PeriodClose.Application.PeriodClose;

public sealed record MonthlyCloseWorkspaceDto(
    MonthlyPeriodDto Period,
    MonthlyClosingSummaryDto? Summary,
    IReadOnlyList<MonthlyCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionMonthlySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryMonthlySnapshotDto> InventorySnapshots,
    IReadOnlyList<BusinessPeriodDto> DayPeriods,
    IReadOnlyList<PeriodAuditLogDto> AuditLog,
    bool CanClose,
    bool CanReopen,
    int OpenDayCount,
    int ClosedDayCount);
