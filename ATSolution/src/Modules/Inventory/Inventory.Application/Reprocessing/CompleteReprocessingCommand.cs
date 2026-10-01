using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Reprocessing;

public sealed record CompleteReprocessingCommand(
    Guid Id,
    decimal RecoveredQuantity,
    decimal ProcessLossQuantity,
    ReprocessingCostBreakdownDto? Costs = null,
    string? Notes = null);
