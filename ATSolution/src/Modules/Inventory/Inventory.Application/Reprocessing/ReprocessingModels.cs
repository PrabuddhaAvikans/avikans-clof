using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Reprocessing;

public sealed class ReprocessingListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? InputScrapLotId { get; set; }
}

public sealed record ReprocessingCostBreakdownDto(
    decimal Labour,
    decimal Electricity,
    decimal Machine,
    decimal Gas,
    decimal Furnace,
    decimal Subcontract,
    decimal Other);

public sealed record ReprocessingBatchDto(
    Guid Id,
    string BatchNumber,
    string Status,
    Guid InputScrapLotId,
    string InputScrapSku,
    string InputScrapName,
    decimal InputQuantity,
    string InputUnit,
    decimal InputUnitCost,
    Guid? WipLotId,
    Guid? IssueMovementId,
    ReprocessingCostBreakdownDto Costs,
    decimal TotalProcessingCost,
    decimal? RecoveredQuantity,
    decimal? ProcessLossQuantity,
    decimal? RecoveredUnitCost,
    Guid? RecoveredLotId,
    string? RecoveredLotSku,
    string? SourceProductionOrderId,
    string? Notes,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset UpdatedAt);

public sealed record ReusableScrapLotDto(
    Guid Id,
    string Sku,
    string Name,
    decimal QuantityAvailable,
    string Unit,
    decimal CostPrice);

public sealed record CreateReprocessingBatchCommand(
    Guid InputScrapLotId,
    decimal InputQuantity,
    ReprocessingCostBreakdownDto? Costs = null,
    string? SourceProductionOrderId = null,
    string? Notes = null,
    string? CreatedBy = null,
    string? CreatedByName = null);

public sealed record CompleteReprocessingCommand(
    Guid Id,
    decimal RecoveredQuantity,
    decimal ProcessLossQuantity,
    ReprocessingCostBreakdownDto? Costs = null,
    string? Notes = null);

public sealed record CancelReprocessingCommand(Guid Id, string? Reason = null);
