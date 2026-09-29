using Inventory.Application.Reprocessing;

namespace Inventory.Api.DTOs.Requests;

public sealed record CompleteReprocessingRequestDto
{
    public decimal RecoveredQuantity { get; init; }
    public decimal ProcessLossQuantity { get; init; }
    public ReprocessingCostBreakdownDto? Costs { get; init; }
    public string? Notes { get; init; }
}
