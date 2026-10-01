using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record MonthOperationalPicture(
    IReadOnlyList<ProductionMonthlySnapshotDto> Production,
    IReadOnlyList<InventoryMonthlySnapshotDto> Inventory,
    MonthlyActivityFigures Figures,
    IReadOnlyList<OperationalIssue> Issues,
    int InProgressProductionJobs);
