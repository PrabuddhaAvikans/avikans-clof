using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Snapshots;

public class WorkerSessionCheckpoint : Entity<Guid>
{
    public Guid BusinessPeriodId { get; private set; }
    public string BusinessDate { get; private set; } = null!;
    public string SessionId { get; private set; } = null!;
    public string WorkerId { get; private set; } = null!;
    public string WorkerName { get; private set; } = null!;
    public string ProductionOrderId { get; private set; } = null!;
    public string OperationId { get; private set; } = null!;
    public string OperationName { get; private set; } = null!;
    public decimal ProgressPercentage { get; private set; }
    public DateTimeOffset StartedAt { get; private set; }
    public DateTimeOffset CheckpointAt { get; private set; }
    public string RuleApplied { get; private set; } = WorkerSessionCloseRules.PauseAndCheckpoint;
    public DateTimeOffset? ResumedAt { get; private set; }
    public string Status { get; private set; } = "paused";

    public static WorkerSessionCheckpoint Create(
        Guid businessPeriodId,
        string businessDate,
        string sessionId,
        string workerId,
        string workerName,
        string productionOrderId,
        string operationId,
        string operationName,
        decimal progressPercentage,
        DateTimeOffset startedAt,
        DateTimeOffset checkpointAt,
        string ruleApplied,
        string status)
    {
        return new WorkerSessionCheckpoint
        {
            Id = Guid.NewGuid(),
            BusinessPeriodId = businessPeriodId,
            BusinessDate = businessDate,
            SessionId = sessionId,
            WorkerId = workerId,
            WorkerName = workerName,
            ProductionOrderId = productionOrderId,
            OperationId = operationId,
            OperationName = operationName,
            ProgressPercentage = progressPercentage,
            StartedAt = startedAt,
            CheckpointAt = checkpointAt,
            RuleApplied = ruleApplied,
            Status = status,
        };
    }
}
