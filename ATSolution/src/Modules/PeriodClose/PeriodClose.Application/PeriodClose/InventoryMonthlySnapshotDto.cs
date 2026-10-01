namespace PeriodClose.Application.PeriodClose;

public sealed record InventoryMonthlySnapshotDto(
    Guid Id,
    Guid MonthlyPeriodId,
    int Year,
    int Month,
    string InventoryItemId,
    string Sku,
    string Name,
    string Unit,
    decimal OpeningQty,
    decimal OpeningValue,
    decimal ReceivedQty,
    decimal ReceivedValue,
    decimal ConsumedQty,
    decimal ConsumedValue,
    decimal AdjustmentQty,
    decimal AdjustmentValue,
    decimal ClosingQty,
    decimal ClosingValue,
    DateTimeOffset RecordedAt);
