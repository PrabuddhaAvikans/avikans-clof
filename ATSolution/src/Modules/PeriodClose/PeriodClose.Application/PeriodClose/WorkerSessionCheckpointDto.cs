namespace PeriodClose.Application.PeriodClose;

public sealed record WorkerSessionCheckpointDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string SessionId,
    string WorkerId,
    string WorkerName,
    string ProductionOrderId,
    string OperationId,
    string OperationName,
    decimal ProgressPercentage,
    DateTimeOffset StartedAt,
    DateTimeOffset CheckpointAt,
    string RuleApplied,
    DateTimeOffset? ResumedAt,
    string Status);
