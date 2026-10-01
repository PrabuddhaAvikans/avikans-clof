namespace Manufacturing.Application.ProductionTracking;

public sealed record TimelineBlockDto(
    Guid Id,
    Guid JobId,
    string JobNumber,
    string Line,
    string Label,
    decimal StartHour,
    decimal EndHour);
