namespace PeriodClose.Application.PeriodClose;

public sealed record DailyTransactionRefsDto(
    IReadOnlyList<string> OrderIds,
    IReadOnlyList<string> InvoiceIds,
    IReadOnlyList<string> PaymentIds,
    IReadOnlyList<string> DeliveryIds,
    IReadOnlyList<string> StockMovementIds,
    IReadOnlyList<string> ProductionJobIds);
