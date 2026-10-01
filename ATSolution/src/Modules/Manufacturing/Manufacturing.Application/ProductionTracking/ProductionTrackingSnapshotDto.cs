namespace Manufacturing.Application.ProductionTracking;

public sealed record ProductionTrackingSnapshotDto(
    ProductionKpisDto Kpis,
    IReadOnlyList<ProductionJobDto> Jobs,
    IReadOnlyList<TimelineBlockDto> Timeline,
    IReadOnlyList<string> Lines,
    IReadOnlyList<SupervisorOptionDto> Supervisors);
