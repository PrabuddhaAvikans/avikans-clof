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
}
