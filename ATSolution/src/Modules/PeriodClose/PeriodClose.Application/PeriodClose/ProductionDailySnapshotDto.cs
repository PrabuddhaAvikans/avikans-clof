namespace PeriodClose.Application.PeriodClose;

public sealed record ProductionDailySnapshotDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string ProductionOrderId,
    string ProductionOrderNumber,
    string OperationId,
    string OperationName,
    string? WorkerId,
    string? WorkerName,
    decimal TotalQty,
    decimal CompletedQty,
    decimal PartialQty,
    decimal ProgressPercentage,
    int WorkedMinutes,
    decimal ProducedQty,
    decimal RejectedQty,
    string? JobStatus,
    string? TaskStatus,
    DateTimeOffset RecordedAt);
