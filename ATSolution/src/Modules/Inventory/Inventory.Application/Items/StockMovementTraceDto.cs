using System.Text.Json;
namespace Inventory.Application.Items;

public sealed record StockMovementTraceDto
{
    public string? SourceInventoryTransactionId { get; init; }
    public string? SourceProductionOrderId { get; init; }
    public string? SourceProductionBatchId { get; init; }
    public string? SourceMaterialLotId { get; init; }
    public string? ReprocessingBatchId { get; init; }
    public string? ParentMaterialTransactionId { get; init; }
    public decimal? UnitCost { get; init; }
    public decimal? CarriedValue { get; init; }
}
