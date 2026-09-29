using ATSolution.Domain.Entities.Common;
using Manufacturing.Domain.Common;

namespace Manufacturing.Domain.Work;

public class EmployeeWorkSession : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public Guid? TaskId { get; private set; }
    public Guid? TaskUnitId { get; private set; }
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public DateOnly BusinessDate { get; private set; }
    public DateTimeOffset StartedAtUtc { get; private set; }
    public DateTimeOffset? EndedAtUtc { get; private set; }
    public DateTimeOffset? LastWorkStartedAtUtc { get; private set; }
    public string Status { get; private set; } = EmployeeWorkSessionStatuses.Working;
    public int WorkedMinutes { get; private set; }
    public int PauseMinutes { get; private set; }
    public string? Remarks { get; private set; }
    public int RowVersion { get; private set; }

    public static EmployeeWorkSession Start(
        Guid jobId,
        Guid? taskId,
        Guid userId,
        string userName,
        DateTimeOffset now)
    {
        return new EmployeeWorkSession
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TaskId = taskId,
            UserId = userId,
            UserName = userName,
            BusinessDate = DateOnly.FromDateTime(now.UtcDateTime),
            StartedAtUtc = now,
            LastWorkStartedAtUtc = now,
            Status = EmployeeWorkSessionStatuses.Working,
            RowVersion = 1,
        };
    }

    public void Pause(DateTimeOffset now, string? reason = null)
    {
        if (Status is EmployeeWorkSessionStatuses.Completed or EmployeeWorkSessionStatuses.Stopped) return;
        AccrueWork(now);
        Status = EmployeeWorkSessionStatuses.Paused;
        LastWorkStartedAtUtc = now;
        Remarks = reason ?? Remarks;
        RowVersion++;
    }

    public void Stop(DateTimeOffset now, string? reason = null)
    {
        AccrueWork(now);
        Status = EmployeeWorkSessionStatuses.Stopped;
        EndedAtUtc = now;
        Remarks = reason ?? Remarks;
        RowVersion++;
    }

    public void Complete(DateTimeOffset now)
    {
        AccrueWork(now);
        Status = EmployeeWorkSessionStatuses.Completed;
        EndedAtUtc = now;
        RowVersion++;
    }

    public void Resume(DateTimeOffset now)
    {
        if (Status is EmployeeWorkSessionStatuses.Completed or EmployeeWorkSessionStatuses.Stopped) return;
        Status = EmployeeWorkSessionStatuses.Working;
        LastWorkStartedAtUtc = now;
        RowVersion++;
    }

    private void AccrueWork(DateTimeOffset now)
    {
        if (Status == EmployeeWorkSessionStatuses.Working && LastWorkStartedAtUtc.HasValue)
        {
            WorkedMinutes += (int)Math.Max(0, (now - LastWorkStartedAtUtc.Value).TotalMinutes);
        }
        else if (Status == EmployeeWorkSessionStatuses.Paused && LastWorkStartedAtUtc.HasValue)
        {
            PauseMinutes += (int)Math.Max(0, (now - LastWorkStartedAtUtc.Value).TotalMinutes);
        }
    }
}
