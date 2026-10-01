using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Snapshots;

public class ProductionMonthlySnapshot : Entity<Guid>
{
    public Guid MonthlyPeriodId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string ProductionOrderId { get; private set; } = null!;
    public string ProductionOrderNumber { get; private set; } = null!;
    public string OperationId { get; private set; } = null!;
    public string OperationName { get; private set; } = null!;
    public decimal TotalQty { get; private set; }
    public decimal CompletedQty { get; private set; }
    public decimal WorkInProgressQty { get; private set; }
    public decimal ProgressPercentage { get; private set; }
    public decimal MaterialConsumed { get; private set; }
    public decimal LaborHours { get; private set; }
    public decimal EstimatedCost { get; private set; }
    public decimal ActualCostToDate { get; private set; }
    public decimal WipCost { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static ProductionMonthlySnapshot Create(
        Guid monthlyPeriodId,
        int year,
        int month,
        DateTimeOffset recordedAt)
    {
        return new ProductionMonthlySnapshot
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            Year = year,
            Month = month,
            ProductionOrderId = string.Empty,
            ProductionOrderNumber = string.Empty,
            OperationId = string.Empty,
            OperationName = string.Empty,
            RecordedAt = recordedAt,
        };
    }

    public static ProductionMonthlySnapshot Capture(
        Guid monthlyPeriodId,
        int year,
        int month,
        string productionOrderId,
        string productionOrderNumber,
        string operationId,
        string operationName,
        decimal totalQty,
        decimal completedQty,
        decimal workInProgressQty,
        decimal progressPercentage,
        decimal materialConsumed,
        decimal laborHours,
        decimal estimatedCost,
        decimal actualCostToDate,
        decimal wipCost,
        DateTimeOffset recordedAt)
    {
        return new ProductionMonthlySnapshot
        {
            Id = Guid.NewGuid(),
            MonthlyPeriodId = monthlyPeriodId,
            Year = year,
            Month = month,
            ProductionOrderId = productionOrderId,
            ProductionOrderNumber = productionOrderNumber,
            OperationId = operationId,
            OperationName = operationName,
            TotalQty = totalQty,
            CompletedQty = completedQty,
            WorkInProgressQty = workInProgressQty,
            ProgressPercentage = progressPercentage,
            MaterialConsumed = materialConsumed,
            LaborHours = laborHours,
            EstimatedCost = estimatedCost,
            ActualCostToDate = actualCostToDate,
            WipCost = wipCost,
            RecordedAt = recordedAt,
        };
    }
}
