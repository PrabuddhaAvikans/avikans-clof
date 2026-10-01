using System.Text.Json;
namespace Manufacturing.Application.Jobs;

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
