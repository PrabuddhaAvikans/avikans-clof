using ATSolution.Domain.Entities.Common;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;

namespace Manufacturing.Domain.Tasks;

public class ManufacturingTask : Entity<Guid>
{
    public Guid JobId { get; private set; }
    public ManufacturingJob Job { get; private set; } = null!;
    public string TaskNumber { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public int Sequence { get; private set; }
    public string Status { get; private set; } = ManufacturingTaskStatuses.Pending;
    public decimal EstimatedHours { get; private set; }
    public decimal ActualHours { get; private set; }
    public string? OperationSnapshotJson { get; private set; }
    public string? Notes { get; private set; }
    public string? Description { get; private set; }
    public bool IsRequired { get; private set; } = true;
    public bool IsEnabled { get; private set; } = true;
    public bool IsQcTask { get; private set; }
    public bool IsTestingTask { get; private set; }
    public bool IsRework { get; private set; }
    public Guid? OriginalTaskId { get; private set; }
    public Guid? ProductOperationId { get; private set; }
    public decimal? LabourCostRate { get; private set; }
    public string? MachineName { get; private set; }
    public decimal? MachineCost { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public string? AssignedToName { get; private set; }
    public decimal RejectedQuantity { get; private set; }
    public decimal ReworkQuantity { get; private set; }
    public decimal WasteQuantity { get; private set; }
    public string PrerequisiteTaskIdsJson { get; private set; } = "[]";
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? PausedAtUtc { get; private set; }
    public ICollection<TaskUnit> Units { get; private set; } = new List<TaskUnit>();
    public ICollection<TaskHistoryEntry> History { get; private set; } = new List<TaskHistoryEntry>();

    public static ManufacturingTask Create(
        Guid jobId,
        string taskNumber,
        string name,
        int sequence,
        decimal estimatedHours,
        int quantity,
        bool isRequired,
        bool isEnabled,
        bool isQcTask,
        Guid? productOperationId,
        string? description,
        decimal? labourCostRate,
        string? machineName,
        decimal? machineCost,
        string? operationSnapshotJson,
        IReadOnlyList<Guid>? prerequisiteTaskIds)
    {
        var task = new ManufacturingTask
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TaskNumber = taskNumber,
            Name = name,
            Sequence = sequence,
            Status = ManufacturingTaskStatuses.Pending,
            EstimatedHours = estimatedHours,
            IsRequired = isRequired,
            IsEnabled = isEnabled,
            IsQcTask = isQcTask,
            ProductOperationId = productOperationId,
            Description = description,
            LabourCostRate = labourCostRate,
            MachineName = machineName,
            MachineCost = machineCost,
            OperationSnapshotJson = operationSnapshotJson,
            PrerequisiteTaskIdsJson = System.Text.Json.JsonSerializer.Serialize(
                prerequisiteTaskIds?.Select(x => x.ToString()).ToList() ?? []),
        };

        for (var i = 1; i <= Math.Max(0, quantity); i++)
            task.Units.Add(TaskUnit.Create(task.Id, i));

        return task;
    }

    public void SetStatus(string status, DateTimeOffset now)
    {
        Status = status;
        if (status == ManufacturingTaskStatuses.InProgress)
        {
            StartedAtUtc ??= now;
            PausedAtUtc = null;
            CompletedAtUtc = null;
        }
        else if (status == ManufacturingTaskStatuses.Paused || status == ManufacturingTaskStatuses.OnHold)
        {
            PausedAtUtc = now;
            CompletedAtUtc = null;
        }
        else if (status is ManufacturingTaskStatuses.Completed or ManufacturingTaskStatuses.Skipped)
        {
            CompletedAtUtc = now;
            PausedAtUtc = null;
        }
        else if (status != ManufacturingTaskStatuses.Completed)
        {
            CompletedAtUtc = null;
        }
    }

    public void SetNotes(string? notes) => Notes = notes;

    public void SetAssignees(Guid? userId, string? userName)
    {
        AssignedToUserId = userId;
        AssignedToName = userName;
    }

    public void AddRejectedWaste(decimal rejected, decimal waste, decimal rework)
    {
        RejectedQuantity += rejected;
        WasteQuantity += waste;
        ReworkQuantity += rework;
    }

    public void SetActualHours(decimal hours) => ActualHours = hours;

    public void AddHistory(TaskHistoryEntry entry) => History.Add(entry);

    public IReadOnlyList<Guid> GetPrerequisiteTaskIds()
    {
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(PrerequisiteTaskIdsJson) ?? [];
            return list.Select(Guid.Parse).ToList();
        }
        catch
        {
            return [];
        }
    }

    public void SetPrerequisiteTaskIds(IEnumerable<Guid> ids)
    {
        PrerequisiteTaskIdsJson = System.Text.Json.JsonSerializer.Serialize(
            ids.Select(x => x.ToString()).ToList());
    }

    public int CompletedUnitCount =>
        Units.Count(u => u.IsComplete || u.IsCancelled);

    public bool AllUnitsDone =>
        Units.Count > 0 && Units.All(u => u.IsComplete || u.IsCancelled);

    public decimal OverallProgress =>
        Units.Count == 0
            ? 0
            : Math.Round(Units.Average(u => u.ProgressPercentage), 2);
}
