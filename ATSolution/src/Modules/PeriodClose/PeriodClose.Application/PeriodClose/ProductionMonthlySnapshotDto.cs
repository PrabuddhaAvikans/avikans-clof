namespace PeriodClose.Application.PeriodClose;

public sealed record ProductionMonthlySnapshotDto(
    Guid Id,
    Guid MonthlyPeriodId,
    int Year,
    int Month,
    string ProductionOrderId,
    string ProductionOrderNumber,
    string OperationId,
    string OperationName,
    decimal TotalQty,
    decimal CompletedQty,
    decimal WorkInProgressQty,
    decimal ProgressPercentage,
    decimal MaterialConsumed,
    decimal LaborHours,
    decimal EstimatedCost,
    decimal ActualCostToDate,
    decimal WipCost,
    DateTimeOffset RecordedAt);
