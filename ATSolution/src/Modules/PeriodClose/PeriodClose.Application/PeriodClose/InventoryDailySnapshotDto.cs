namespace PeriodClose.Application.PeriodClose;

public sealed record InventoryDailySnapshotDto(
    Guid Id,
    Guid BusinessPeriodId,
    string BusinessDate,
    string InventoryItemId,
    string Sku,
    string Name,
    string Unit,
    decimal OpeningQty,
    decimal Receipts,
    decimal Returns,
    decimal ProductionOutput,
    decimal Issues,
    decimal Consumption,
    decimal Deliveries,
    decimal Adjustments,
    decimal ClosingQty,
    IReadOnlyList<string> MovementIds,
    DateTimeOffset RecordedAt);
