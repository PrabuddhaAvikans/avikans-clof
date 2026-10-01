namespace PeriodClose.Application.PeriodClose;

public sealed record CloseDayResultDto(
    BusinessPeriodDto Period,
    BusinessPeriodDto NextPeriod,
    DailyClosingSummaryDto Summary,
    IReadOnlyList<DayCloseValidationIssueDto> Validations,
    IReadOnlyList<ProductionDailySnapshotDto> ProductionSnapshots,
    IReadOnlyList<InventoryDailySnapshotDto> InventorySnapshots,
    IReadOnlyList<WorkerSessionCheckpointDto> SessionCheckpoints);
