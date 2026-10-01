using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record DailyActivityFigures(
    int OrdersCreated,
    int ProductionJobs,
    decimal CompletedProductionQty,
    decimal PartialProductionQty,
    int Invoices,
    decimal InvoiceTotal,
    int Payments,
    decimal PaymentTotal,
    int Deliveries,
    int MaterialIssues,
    int MaterialReturns,
    int InventoryMovementCount,
    decimal QuotationValue,
    decimal SalesOrderValue,
    decimal CreditNoteTotal,
    decimal OpeningReceivable,
    decimal ClosingReceivable,
    decimal OutstandingAmount,
    DailyTransactionRefsDto TransactionRefs);
