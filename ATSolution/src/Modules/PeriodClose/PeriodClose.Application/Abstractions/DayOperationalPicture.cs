using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record DayOperationalPicture(
    IReadOnlyList<EmployeeDayWorkSummaryDto> Employees,
    IReadOnlyList<ProductionDailySnapshotDto> Production,
    IReadOnlyList<InventoryDailySnapshotDto> Inventory,
    DailyActivityFigures Figures,
    IReadOnlyList<OperationalIssue> Issues,
    int ActiveSessionCount,
    IReadOnlyList<SessionCheckpointSeed> ActiveSessions);
