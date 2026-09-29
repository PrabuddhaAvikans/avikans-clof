using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Summaries;

public class MonthlyClosingSummary : Entity<Guid>
{
    public Guid MonthlyPeriodId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string BranchId { get; private set; } = null!;
    public decimal SalesTotal { get; private set; }
    public decimal PurchaseTotal { get; private set; }
    public decimal PaymentTotal { get; private set; }
    public decimal ExpenseTotal { get; private set; }
    public decimal InventoryValue { get; private set; }
    public decimal WipValue { get; private set; }
    public decimal CostOfGoodsSold { get; private set; }
    public decimal GrossProfit { get; private set; }
    public decimal RawMaterials { get; private set; }
    public decimal Labour { get; private set; }
    public decimal Production { get; private set; }
    public decimal Waste { get; private set; }
    public decimal ReusableWaste { get; private set; }
    public decimal Overhead { get; private set; }
    public decimal CreditNotes { get; private set; }
    public decimal NetMargin { get; private set; }
    public string TransactionRefsJson { get; private set; } = "{}";
    public DateTimeOffset CreatedAt { get; private set; }

    public static MonthlyClosingSummary Create(
        Guid monthlyPeriodId,
        int year,
        int month,
        string branchId,
        string transactionRefsJson,
        DateTimeOffset createdAt)
    {
        return new MonthlyClosingSummary
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            Year = year,
            Month = month,
            BranchId = branchId,
            TransactionRefsJson = transactionRefsJson,
            CreatedAt = createdAt,
        };
    }
}
