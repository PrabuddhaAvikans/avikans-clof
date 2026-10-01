namespace PeriodClose.Application.PeriodClose;

public sealed record CloseMonthResultDto(
    MonthlyPeriodDto Period,
    MonthlyPeriodDto NextPeriod,
    MonthlyClosingSummaryDto Summary,
    IReadOnlyList<MonthlyCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionMonthlySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryMonthlySnapshotDto> InventorySnapshots);
