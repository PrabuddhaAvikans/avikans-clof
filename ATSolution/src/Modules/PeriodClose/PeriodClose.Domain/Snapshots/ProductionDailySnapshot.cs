using ATSolution.Domain.Entities.Common;

namespace PeriodClose.Domain.Snapshots;

public class ProductionDailySnapshot : Entity<Guid>
{
    public Guid BusinessPeriodId { get; private set; }
    public string BusinessDate { get; private set; } = null!;
    public string ProductionOrderId { get; private set; } = null!;
    public string ProductionOrderNumber { get; private set; } = null!;
    public string OperationId { get; private set; } = null!;
    public string OperationName { get; private set; } = null!;
    public string? WorkerId { get; private set; }
    public string? WorkerName { get; private set; }
    public decimal TotalQty { get; private set; }
    public decimal CompletedQty { get; private set; }
    public decimal PartialQty { get; private set; }
    public decimal ProgressPercentage { get; private set; }
    public int WorkedMinutes { get; private set; }
    public decimal ProducedQty { get; private set; }
    public decimal RejectedQty { get; private set; }
    public string? JobStatus { get; private set; }
    public string? TaskStatus { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    public static ProductionDailySnapshot Create(
        Guid businessPeriodId,
        string businessDate,
        DateTimeOffset recordedAt)
    {
        return new ProductionDailySnapshot
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            ProductionOrderId = string.Empty,
            ProductionOrderNumber = string.Empty,
            OperationId = string.Empty,
            OperationName = string.Empty,
            RecordedAt = recordedAt,
        };
    }

    public static ProductionDailySnapshot Capture(
        Guid businessPeriodId,
        string businessDate,
        string productionOrderId,
        string productionOrderNumber,
        string operationId,
        string operationName,
        string? workerId,
        string? workerName,
        decimal totalQty,
        decimal completedQty,
        decimal partialQty,
        decimal progressPercentage,
        int workedMinutes,
        decimal producedQty,
        decimal rejectedQty,
        string? jobStatus,
        string? taskStatus,
        DateTimeOffset recordedAt)
    {
        return new ProductionDailySnapshot
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            ProductionOrderId = productionOrderId,
            ProductionOrderNumber = productionOrderNumber,
            OperationId = operationId,
            OperationName = operationName,
            WorkerId = workerId,
            WorkerName = workerName,
            TotalQty = totalQty,
            CompletedQty = completedQty,
            PartialQty = partialQty,
            ProgressPercentage = progressPercentage,
            WorkedMinutes = workedMinutes,
            ProducedQty = producedQty,
            RejectedQty = rejectedQty,
            JobStatus = jobStatus,
            TaskStatus = taskStatus,
            RecordedAt = recordedAt,
        };
    }
}
