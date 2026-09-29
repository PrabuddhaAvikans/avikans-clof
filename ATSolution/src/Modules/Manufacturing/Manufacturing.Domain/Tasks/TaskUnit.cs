using ATSolution.Domain.Entities.Common;
using Manufacturing.Domain.Common;

namespace Manufacturing.Domain.Tasks;

public class TaskUnit : Entity<Guid>
{
    public Guid TaskId { get; private set; }
    public ManufacturingTask Task { get; private set; } = null!;
    public int UnitNo { get; private set; }
    public decimal ProgressPercentage { get; private set; }
    public string Status { get; private set; } = TaskUnitStatuses.Pending;
    public ICollection<TaskUnitAssignment> Assignments { get; private set; } = new List<TaskUnitAssignment>();

    public static TaskUnit Create(Guid taskId, int unitNo)
    {
        return new TaskUnit
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UnitNo = unitNo,
            ProgressPercentage = 0,
            Status = TaskUnitStatuses.Pending,
        };
    }

    public void SetProgress(decimal progressPercentage)
    {
        ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
        SyncStatusFromProgress();
    }

    public void Cancel()
    {
        Status = TaskUnitStatuses.Cancelled;
    }

    public void SyncStatusFromProgress()
    {
        if (Status == TaskUnitStatuses.Cancelled) return;
        if (ProgressPercentage >= 100)
            Status = TaskUnitStatuses.Completed;
        else if (ProgressPercentage > 0 || Assignments.Count > 0)
            Status = TaskUnitStatuses.InProgress;
        else if (Assignments.Count > 0)
            Status = TaskUnitStatuses.Assigned;
        else
            Status = TaskUnitStatuses.Pending;
    }

    public bool IsComplete =>
        Status == TaskUnitStatuses.Completed || ProgressPercentage >= 100;

    public bool IsCancelled => Status == TaskUnitStatuses.Cancelled;

    public bool IsUnallocated =>
        Assignments.Count == 0 && Status == TaskUnitStatuses.Pending;
}
