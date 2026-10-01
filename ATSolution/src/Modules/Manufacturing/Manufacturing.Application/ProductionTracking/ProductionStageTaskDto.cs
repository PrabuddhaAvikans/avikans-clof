namespace Manufacturing.Application.ProductionTracking;

public sealed record ProductionStageTaskDto(
    Guid Id,
    string Name,
    int Sequence,
    string Status,
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    decimal ProgressPercent,
    DateTimeOffset? CompletedAt);
