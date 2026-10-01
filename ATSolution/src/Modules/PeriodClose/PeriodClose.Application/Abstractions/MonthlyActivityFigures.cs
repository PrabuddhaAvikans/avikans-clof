using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record MonthlyActivityFigures(
    decimal SalesTotal,
    decimal PurchaseTotal,
    decimal PaymentTotal,
    decimal ExpenseTotal,
    decimal InventoryValue,
    decimal WipValue,
    decimal CostOfGoodsSold,
    decimal GrossProfit,
    decimal RawMaterials,
    decimal Labour,
    decimal Production,
    decimal Waste,
    decimal ReusableWaste,
    decimal Overhead,
    decimal CreditNotes,
    decimal NetMargin,
    MonthlyTransactionRefsDto TransactionRefs);
