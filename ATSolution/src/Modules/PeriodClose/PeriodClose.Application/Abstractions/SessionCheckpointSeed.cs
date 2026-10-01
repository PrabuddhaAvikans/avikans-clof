using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record SessionCheckpointSeed(
    string SessionId,
    string WorkerId,
    string WorkerName,
    string ProductionOrderId,
    string OperationId,
    string OperationName,
    decimal ProgressPercentage,
    DateTimeOffset StartedAt,
    string Status);
