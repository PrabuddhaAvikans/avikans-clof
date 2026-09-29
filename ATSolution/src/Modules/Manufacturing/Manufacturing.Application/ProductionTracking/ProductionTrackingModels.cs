using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.ProductionTracking;

public sealed class ProductionTrackingFilters : PaginatedRequest
{
    public string? Line { get; set; }
    public Guid? SupervisorId { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public bool? DelayedOnly { get; set; }
}

public sealed record ProductionStageTaskDto(
    Guid Id,
    string Name,
    int Sequence,
    string Status,
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    decimal ProgressPercent,
    DateTimeOffset? CompletedAt);

public sealed record ProductionJobDto(
    Guid Id,
    string JobNumber,
    string SalesOrderNumber,
    string ProductName,
    string ProductSku,
    string? ProductImageUrl,
    string CustomerName,
    decimal Quantity,
    string Priority,
    string CurrentTaskName,
    string Line,
    Guid SupervisorId,
    string SupervisorName,
    DateTimeOffset StartDate,
    DateTimeOffset DueDate,
    decimal CompletionPercent,
    string Status,
    string StatusLabel,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<ProductionStageTaskDto> Stages,
    decimal MaterialIssued,
    decimal MaterialConsumed,
    string MaterialUnit,
    bool MaterialsReady,
    IReadOnlyList<string> Blockers,
    decimal LaborHours,
    decimal OvertimeHours,
    decimal NormalOvertimeHours,
    decimal DoubleOvertimeHours,
    decimal LaborCost,
    decimal OvertimeCost,
    decimal EstimatedCost,
    decimal ActualCost,
    int QualityOpen,
    int QualityClosed,
    decimal ElapsedHours,
    decimal RemainingHours,
    int? OverdueDays);

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

public sealed record TimelineBlockDto(
    Guid Id,
    Guid JobId,
    string JobNumber,
    string Line,
    string Label,
    decimal StartHour,
    decimal EndHour);

public sealed record ProductionTrackingSnapshotDto(
    ProductionKpisDto Kpis,
    IReadOnlyList<ProductionJobDto> Jobs,
    IReadOnlyList<TimelineBlockDto> Timeline,
    IReadOnlyList<string> Lines,
    IReadOnlyList<SupervisorOptionDto> Supervisors);

public sealed record SupervisorOptionDto(Guid Id, string Name);
