using ATSolution.Domain.Entities.Common;

namespace Manufacturing.Domain.Tasks;

public class TaskHistoryEntry : Entity<Guid>
{
    public Guid TaskId { get; private set; }
    public ManufacturingTask Task { get; private set; } = null!;
    public Guid UserId { get; private set; }
    public string UserName { get; private set; } = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? OldStatus { get; private set; }
    public string? NewStatus { get; private set; }
    public string? Comments { get; private set; }

    public static TaskHistoryEntry Create(
        Guid taskId,
        Guid userId,
        string userName,
        string action,
        string? oldStatus,
        string? newStatus,
        string? comments)
    {
        return new TaskHistoryEntry
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            UserName = userName,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            Action = action,
            OldStatus = oldStatus,
            NewStatus = newStatus,
            Comments = comments,
        };
    }
}
