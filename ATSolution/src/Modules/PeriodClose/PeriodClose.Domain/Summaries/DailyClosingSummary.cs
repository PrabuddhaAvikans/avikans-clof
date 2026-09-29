using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Summaries;

public class DailyClosingSummary : Entity<Guid>
{
    public Guid BusinessPeriodId { get; private set; }
    public string BusinessDate { get; private set; } = null!;
    public string BranchId { get; private set; } = null!;
    public int OrdersCreated { get; private set; }
    public int ProductionJobs { get; private set; }
    public decimal CompletedProductionQty { get; private set; }
    public decimal PartialProductionQty { get; private set; }
    public int Invoices { get; private set; }
    public decimal InvoiceTotal { get; private set; }
    public int Payments { get; private set; }
    public decimal PaymentTotal { get; private set; }
    public int Deliveries { get; private set; }
    public int MaterialIssues { get; private set; }
    public int MaterialReturns { get; private set; }
    public int InventoryMovementCount { get; private set; }
    public decimal QuotationValue { get; private set; }
    public decimal SalesOrderValue { get; private set; }
    public decimal CreditNoteTotal { get; private set; }
    public decimal CashPayments { get; private set; }
    public decimal CardPayments { get; private set; }
    public decimal BankPayments { get; private set; }
    public decimal AdvancePayments { get; private set; }
    public decimal Refunds { get; private set; }
    public decimal OpeningReceivable { get; private set; }
    public decimal ClosingReceivable { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string TransactionRefsJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }

    public static DailyClosingSummary Create(
        Guid businessPeriodId,
        string businessDate,
        string branchId,
        string transactionRefsJson,
        DateTimeOffset createdAt)
    {
        return new DailyClosingSummary
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            BranchId = branchId,
            TransactionRefsJson = transactionRefsJson,
            CreatedAt = createdAt,
            ClosingReceivable = 0,
            OutstandingAmount = 0,
        };
    }
}
