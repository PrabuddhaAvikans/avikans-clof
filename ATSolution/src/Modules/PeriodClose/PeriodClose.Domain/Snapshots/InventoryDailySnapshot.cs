using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Snapshots;

public class InventoryDailySnapshot : Entity<Guid>
{
    public Guid BusinessPeriodId { get; private set; }
    public string BusinessDate { get; private set; } = null!;
    public string InventoryItemId { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public decimal OpeningQty { get; private set; }
    public decimal Receipts { get; private set; }
    public decimal Returns { get; private set; }
    public decimal ProductionOutput { get; private set; }
    public decimal Issues { get; private set; }
    public decimal Consumption { get; private set; }
    public decimal Deliveries { get; private set; }
    public decimal Adjustments { get; private set; }
    public decimal ClosingQty { get; private set; }
    public string MovementIdsJson { get; private set; } = "[]";
    public DateTimeOffset RecordedAt { get; private set; }

    public static InventoryDailySnapshot Create(
        Guid businessPeriodId,
        string businessDate,
        DateTimeOffset recordedAt)
    {
        return new InventoryDailySnapshot
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            InventoryItemId = string.Empty,
            Sku = string.Empty,
            Name = string.Empty,
            Unit = string.Empty,
            MovementIdsJson = "[]",
            RecordedAt = recordedAt,
        };
    }

    public static InventoryDailySnapshot Capture(
        Guid businessPeriodId,
        string businessDate,
        string inventoryItemId,
        string sku,
        string name,
        string unit,
        decimal openingQty,
        decimal receipts,
        decimal returns,
        decimal productionOutput,
        decimal issues,
        decimal consumption,
        decimal deliveries,
        decimal adjustments,
        decimal closingQty,
        string movementIdsJson,
        DateTimeOffset recordedAt)
    {
        return new InventoryDailySnapshot
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            InventoryItemId = inventoryItemId,
            Sku = sku,
            Name = name,
            Unit = unit,
            OpeningQty = openingQty,
            Receipts = receipts,
            Returns = returns,
            ProductionOutput = productionOutput,
            Issues = issues,
            Consumption = consumption,
            Deliveries = deliveries,
            Adjustments = adjustments,
            ClosingQty = closingQty,
            MovementIdsJson = string.IsNullOrWhiteSpace(movementIdsJson) ? "[]" : movementIdsJson,
            RecordedAt = recordedAt,
        };
    }
}
