using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Snapshots;

public class InventoryMonthlySnapshot : Entity<Guid>
{
    public Guid MonthlyPeriodId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string InventoryItemId { get; private set; } = null!;
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public decimal OpeningQty { get; private set; }
    public decimal OpeningValue { get; private set; }
    public decimal ReceivedQty { get; private set; }
    public decimal ReceivedValue { get; private set; }
    public decimal ConsumedQty { get; private set; }
    public decimal ConsumedValue { get; private set; }
    public decimal AdjustmentQty { get; private set; }
    public decimal AdjustmentValue { get; private set; }
    public decimal ClosingQty { get; private set; }
    public decimal ClosingValue { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static InventoryMonthlySnapshot Create(
        Guid monthlyPeriodId,
        int year,
        int month,
        DateTimeOffset recordedAt)
    {
        return new InventoryMonthlySnapshot
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            Year = year,
            Month = month,
            InventoryItemId = string.Empty,
            Sku = string.Empty,
            Name = string.Empty,
            Unit = string.Empty,
            RecordedAt = recordedAt,
        };
    }

    public static InventoryMonthlySnapshot Capture(
        Guid monthlyPeriodId,
        int year,
        int month,
        string inventoryItemId,
        string sku,
        string name,
        string unit,
        decimal openingQty,
        decimal openingValue,
        decimal receivedQty,
        decimal receivedValue,
        decimal consumedQty,
        decimal consumedValue,
        decimal adjustmentQty,
        decimal adjustmentValue,
        decimal closingQty,
        decimal closingValue,
        DateTimeOffset recordedAt)
    {
        return new InventoryMonthlySnapshot
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            Year = year,
            Month = month,
            InventoryItemId = inventoryItemId,
            Sku = sku,
            Name = name,
            Unit = unit,
            OpeningQty = openingQty,
            OpeningValue = openingValue,
            ReceivedQty = receivedQty,
            ReceivedValue = receivedValue,
            ConsumedQty = consumedQty,
            ConsumedValue = consumedValue,
            AdjustmentQty = adjustmentQty,
            AdjustmentValue = adjustmentValue,
            ClosingQty = closingQty,
            ClosingValue = closingValue,
            RecordedAt = recordedAt,
        };
    }
}
