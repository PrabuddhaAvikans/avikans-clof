using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Reprocessing;

public sealed record CreateReprocessingBatchCommand(
    Guid InputScrapLotId,
    decimal InputQuantity,
    ReprocessingCostBreakdownDto? Costs = null,
    string? SourceProductionOrderId = null,
    string? Notes = null,
    string? CreatedBy = null,
    string? CreatedByName = null);
