namespace Manufacturing.Application.ProductionTracking;

public sealed record ProductionKpisDto(
    int JobsInProduction,
    int JobsInProductionTrend,
    int OnHold,
    int OnHoldTrend,
    int InQualityCheck,
    int InQualityCheckTrend,
    int DelayedJobs,
    int DelayedJobsTrend,
    int ReadyToShip,
    int ReadyToShipTrend);
