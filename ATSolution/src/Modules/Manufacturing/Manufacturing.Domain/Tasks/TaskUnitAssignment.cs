using ATSolution.Domain.Entities.Common;
using Manufacturing.Domain.Common;

namespace Manufacturing.Domain.Tasks;

public class TaskUnitAssignment : Entity<Guid>
{
    public Guid TaskUnitId { get; private set; }
    public TaskUnit TaskUnit { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public decimal ContributionPercentage { get; private set; } = 100m;
    public string Status { get; private set; } = TaskUnitAssignmentStatuses.Assigned;
    public decimal ActualHours { get; private set; }
    public decimal NormalOvertimeHours { get; private set; }
    public decimal DoubleOvertimeHours { get; private set; }
    public decimal LaborCost { get; private set; }
    public decimal RejectedQuantity { get; private set; }
    public decimal WasteQuantity { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? PausedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    public decimal OvertimeHours => NormalOvertimeHours + DoubleOvertimeHours;

    public static TaskUnitAssignment Create(
        Guid taskUnitId,
        Guid userId,
        string userName,
        decimal contributionPercentage,
        string status,
        DateTimeOffset? startedAtUtc = null)
    {
        return new TaskUnitAssignment
        {
            Id = Guid.NewGuid(),
            TaskUnitId = taskUnitId,
            UserId = userId,
            UserName = userName,
            ContributionPercentage = contributionPercentage,
            Status = status,
            StartedAtUtc = startedAtUtc,
        };
    }

    public void ApplyProgress(
        decimal contributionPercentage,
        string status,
        decimal actualHours,
        decimal normalOvertimeHours,
        decimal doubleOvertimeHours,
        decimal laborCost,
        decimal rejectedQuantity,
        decimal wasteQuantity,
        DateTimeOffset now)
    {
        ContributionPercentage = contributionPercentage;
        Status = status;
        ActualHours = actualHours;
        NormalOvertimeHours = normalOvertimeHours;
        DoubleOvertimeHours = doubleOvertimeHours;
        LaborCost = laborCost;
        RejectedQuantity = rejectedQuantity;
        WasteQuantity = wasteQuantity;
        StartedAtUtc ??= now;
        if (status is TaskUnitAssignmentStatuses.Paused or TaskUnitAssignmentStatuses.OnHold)
            PausedAtUtc = now;
        else
            PausedAtUtc = null;

        CompletedAtUtc = status == TaskUnitAssignmentStatuses.Completed ? now : null;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        if (Status == TaskUnitAssignmentStatuses.Completed) return;
        Status = status;
        if (status == TaskUnitAssignmentStatuses.InProgress)
        {
            PausedAtUtc = null;
            StartedAtUtc ??= now;
        }
        else if (status is TaskUnitAssignmentStatuses.Paused or TaskUnitAssignmentStatuses.OnHold)
        {
            PausedAtUtc = now;
        }
    }
}
