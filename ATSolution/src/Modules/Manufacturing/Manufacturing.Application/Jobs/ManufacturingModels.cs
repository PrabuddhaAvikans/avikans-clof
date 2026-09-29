using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Manufacturing.Application.Jobs;

public sealed class ManufacturingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? AssignedTo { get; set; }
    public string? Priority { get; set; }
}

public sealed record TaskActionActorDto(Guid UserId, string UserName);

public sealed record MaterialRequirementDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemSku,
    string InventoryItemName,
    decimal RequiredQuantity,
    decimal ReservedQuantity,
    decimal IssuedQuantity,
    string Unit,
    string Status,
    Guid? IssuedFromInventoryItemId,
    Guid? IssuedStockMovementId,
    decimal? IssuedUnitCost);

public sealed record TaskUnitAssignmentDto(
    Guid Id,
    Guid TaskUnitId,
    Guid UserId,
    string UserName,
    decimal ContributionPercentage,
    string Status,
    decimal ActualHours,
    decimal OvertimeHours,
    decimal NormalOvertimeHours,
    decimal DoubleOvertimeHours,
    decimal LaborCost,
    decimal RejectedQuantity,
    decimal WasteQuantity,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt);

public sealed record TaskUnitDto(
    Guid Id,
    Guid TaskId,
    int UnitNo,
    decimal ProgressPercentage,
    string Status,
    IReadOnlyList<TaskUnitAssignmentDto> Assignments);

public sealed record TaskContributorDto(
    Guid UserId,
    string UserName,
    decimal ContributionPercent,
    decimal Quantity,
    decimal RejectedQuantity,
    decimal WasteQuantity,
    decimal ActualHours,
    decimal OvertimeHours,
    decimal NormalOvertimeHours,
    decimal DoubleOvertimeHours,
    decimal LaborCost,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    decimal? ProgressPercentage,
    decimal? CompletedQuantity,
    decimal? InProgressQuantity);

public sealed record ManufacturingTaskHistoryEntryDto(
    Guid Id,
    Guid TaskId,
    Guid UserId,
    string UserName,
    DateTimeOffset OccurredAt,
    string Action,
    string? OldStatus,
    string? NewStatus,
    string? Comments);

public sealed record TaskMaterialUsageDto(
    Guid Id,
    Guid InventoryItemId,
    string InventoryItemSku,
    string InventoryItemName,
    decimal Quantity,
    string Unit,
    decimal? Cost);

public sealed record ManufacturingTaskDto(
    Guid Id,
    string TaskNumber,
    Guid ProductionJobId,
    Guid? ProductOperationId,
    int Sequence,
    string Name,
    string? Description,
    bool IsRequired,
    bool IsEnabled,
    bool IsQcTask,
    bool IsTestingTask,
    bool IsRework,
    Guid? OriginalTaskId,
    decimal EstimatedHours,
    decimal? ActualHours,
    decimal? OvertimeHours,
    decimal? NormalOvertimeHours,
    decimal? DoubleOvertimeHours,
    decimal? LabourCostRate,
    string? MachineName,
    decimal? MachineCost,
    decimal? ActualCost,
    Guid? AssignedTo,
    string? AssignedToName,
    Guid? OperatorId,
    string? OperatorName,
    IReadOnlyList<TaskContributorDto> Contributors,
    IReadOnlyList<TaskUnitDto> Units,
    decimal PlannedQuantity,
    decimal CompletedQuantity,
    decimal PartiallyCompletedQuantity,
    decimal RejectedQuantity,
    decimal ReworkQuantity,
    decimal WasteQuantity,
    decimal StartedQuantity,
    decimal OverallProgress,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? PausedAt,
    string? Notes,
    IReadOnlyList<Guid> PrerequisiteTaskIds,
    IReadOnlyList<ManufacturingTaskHistoryEntryDto> History,
    IReadOnlyList<TaskMaterialUsageDto> MaterialsUsed,
    string? Workstation);

public sealed record ManufacturingReworkDto(
    Guid Id,
    string ReworkNumber,
    Guid OriginalTaskId,
    Guid ReworkTaskId,
    string Reason,
    decimal Quantity,
    decimal? AdditionalTimeHours,
    IReadOnlyList<TaskMaterialUsageDto>? AdditionalMaterials,
    decimal? AdditionalCost,
    string? Result,
    DateTimeOffset CreatedAt,
    DateTimeOffset? CompletedAt,
    string? Notes);

public sealed record QualityInspectionChecklistItemDto(
    Guid Id,
    string Name,
    bool? Passed,
    string? Notes);

public sealed record QualityInspectionDto(
    Guid Id,
    string InspectionNumber,
    Guid InspectorId,
    string InspectorName,
    string Status,
    IReadOnlyList<QualityInspectionChecklistItemDto> ChecklistItems,
    DateTimeOffset? InspectedAt,
    string? Notes);

public sealed record ProductionMaterialOutcomeDto(
    decimal FinishedMaterialQuantity,
    decimal ReusableScrapQuantity,
    decimal RecoverableQuantity,
    decimal PermanentWasteQuantity,
    DateTimeOffset PostedAt,
    IReadOnlyList<string> ScrapLotIds,
    IReadOnlyList<string> RecoverableLotIds);

public sealed record ManufacturingJobDto(
    Guid Id,
    string JobNumber,
    Guid SalesOrderId,
    string SalesOrderNumber,
    Guid CustomerId,
    string CustomerName,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    Guid? ProductVersionId,
    string ProductVersionLabel,
    decimal Quantity,
    string Status,
    string Priority,
    IReadOnlyList<ManufacturingTaskDto> Tasks,
    IReadOnlyList<ManufacturingReworkDto> Reworks,
    IReadOnlyList<MaterialRequirementDto> MaterialRequirements,
    QualityInspectionDto? QualityInspection,
    DateTimeOffset PlannedStartDate,
    DateTimeOffset PlannedEndDate,
    DateTimeOffset? ActualStartDate,
    DateTimeOffset? ActualEndDate,
    decimal ProgressPercent,
    decimal EstimatedCost,
    decimal ActualCost,
    Guid? AssignedTo,
    string? AssignedToName,
    string? Notes,
    ProductionMaterialOutcomeDto? MaterialOutcome,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateManufacturingJobCommand(
    Guid SalesOrderId,
    Guid ProductId,
    Guid? ProductVersionId,
    decimal Quantity,
    string Priority,
    DateTimeOffset PlannedStartDate,
    DateTimeOffset PlannedEndDate,
    Guid? AssignedTo,
    string? Notes,
    string? CreatedBy,
    string? CreatedByName);

public sealed record UpdateManufacturingJobCommand(
    Guid Id,
    string? Priority,
    DateTimeOffset? PlannedStartDate,
    DateTimeOffset? PlannedEndDate,
    Guid? AssignedTo,
    string? AssignedToName,
    string? Notes,
    string? Status,
    QualityInspectionDto? QualityInspection);

public sealed record ProductionCompletionInputDto(
    decimal FinishedMaterialQuantity,
    decimal ReusableScrapQuantity,
    decimal? RecoverableQuantity,
    decimal PermanentWasteQuantity,
    string? Notes);

public sealed record TaskContributorInputDto(
    Guid UserId,
    string UserName,
    decimal? ContributionPercent,
    decimal? Quantity,
    decimal? RejectedQuantity,
    decimal? WasteQuantity,
    decimal? ActualHours,
    decimal? OvertimeHours,
    decimal? NormalOvertimeHours,
    decimal? DoubleOvertimeHours,
    decimal? ProgressPercentage,
    IReadOnlyList<int>? UnitNos);

public sealed record BulkCompleteTaskEntryDto(
    Guid TaskId,
    decimal? CompletedQuantity,
    decimal? RejectedQuantity,
    decimal? WasteQuantity,
    IReadOnlyList<TaskContributorInputDto>? Contributors,
    decimal? ActualHours,
    decimal? NormalOvertimeHours,
    decimal? DoubleOvertimeHours,
    string? Notes);

public sealed record BulkCompleteTasksInputDto(
    IReadOnlyList<BulkCompleteTaskEntryDto>? Tasks,
    IReadOnlyList<Guid>? TaskIds,
    string? Notes);

public sealed record ManufacturingTaskActionDto
{
    public string Type { get; init; } = null!;
    public Guid? TaskId { get; init; }
    public Guid? AssignedTo { get; init; }
    public string? AssignedToName { get; init; }
    public Guid? OperatorId { get; init; }
    public string? OperatorName { get; init; }
    public string? MachineName { get; init; }
    public decimal? QuantityStarted { get; init; }
    public IReadOnlyList<TaskContributorInputDto>? Contributors { get; init; }
    public string? Notes { get; init; }
    public decimal? CompletedQuantity { get; init; }
    public decimal? RejectedQuantity { get; init; }
    public decimal? WasteQuantity { get; init; }
    public decimal? ReworkQuantity { get; init; }
    public decimal? ActualHours { get; init; }
    public decimal? OvertimeHours { get; init; }
    public decimal? NormalOvertimeHours { get; init; }
    public decimal? DoubleOvertimeHours { get; init; }
    public IReadOnlyList<TaskMaterialUsageDto>? MaterialsUsed { get; init; }
    public string? Reason { get; init; }
    public decimal? Quantity { get; init; }
    public string? Result { get; init; }
    public QualityInspectionDto? Inspection { get; init; }
    public Guid? FailedTaskId { get; init; }
    public JsonElement? ActiveSessionSwitch { get; init; }
}

internal sealed record BomItemJson(
    Guid Id,
    int Sequence,
    Guid InventoryItemId,
    string InventoryItemName,
    string Sku,
    decimal Quantity,
    string Unit,
    decimal WastePercent,
    decimal RequiredQuantity,
    bool IsRequired);

internal sealed record ProductOperationJson(
    Guid Id,
    string Name,
    int Sequence,
    string? Description,
    string Workstation,
    decimal EstimatedHours,
    decimal? LabourCostRate,
    string? MachineName,
    decimal? MachineCost,
    bool IsRequired,
    bool IsEnabled,
    string? Notes,
    IReadOnlyList<Guid>? PrerequisiteOperationIds,
    bool? IsQualityCheck);
