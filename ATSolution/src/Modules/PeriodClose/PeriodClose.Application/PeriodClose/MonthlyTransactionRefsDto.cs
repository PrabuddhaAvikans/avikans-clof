namespace PeriodClose.Application.PeriodClose;

public sealed record MonthlyTransactionRefsDto(
    IReadOnlyList<string> InvoiceIds,
    IReadOnlyList<string> PaymentIds,
    IReadOnlyList<string> PurchaseIds,
    IReadOnlyList<string> ExpenseIds,
    IReadOnlyList<string> InventorySnapshotIds,
    IReadOnlyList<string> ProductionSnapshotIds);
