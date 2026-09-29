using ATSolution.Application;
using Manufacturing.Application.Common;
using Manufacturing.Application.Jobs;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;

namespace Manufacturing.Application.Services;

internal static class TaskActionProcessor
{
    public static void Apply(
        ManufacturingJob job,
        ManufacturingTaskActionDto action,
        TaskActionActorDto actor,
        QualityInspectionDto? currentInspection)
    {
        switch (action.Type)
        {
            case "start":
                ApplyStart(job, action, actor);
                break;
            case "pause":
                ApplyPause(job, action, actor);
                break;
            case "resume":
                ApplyResume(job, action, actor);
                break;
            case "hold":
                ApplyHold(job, action, actor);
                break;
            case "complete":
                ApplyComplete(job, action, actor);
                break;
            case "skip":
                ApplySkip(job, action, actor);
                break;
            case "block":
                ApplyBlock(job, action, actor);
                break;
            case "notes":
                ApplyNotes(job, action, actor);
                break;
            case "rework":
                ApplyRework(job, action, actor);
                break;
            case "qc":
                ApplyQc(job, action, actor, currentInspection);
                break;
            default:
                ManufacturingErrors.InvalidState("Unknown manufacturing task action.");
                break;
        }

        ManufacturingJobHelper.RefreshJobDerivedFields(job, currentInspection);
    }

    private static ManufacturingTask RequireTask(ManufacturingJob job, Guid taskId)
    {
        return job.Tasks.FirstOrDefault(t => t.Id == taskId)
            ?? throw new NotFoundException($"Manufacturing task '{taskId}' was not found.");
    }

    private static void ApplyStart(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.Status is ManufacturingTaskStatuses.OnHold or ManufacturingTaskStatuses.Paused)
        {
            ApplyResume(job, action with { Type = "resume" }, actor);
            return;
        }

        if (task.Status is not ManufacturingTaskStatuses.Ready and not ManufacturingTaskStatuses.ReworkRequired)
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" must be Ready before it can be started.");
        }

        if (!ManufacturingJobHelper.ArePrerequisitesMet(task, job.Tasks.ToList()))
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" is waiting on prerequisite tasks.");
        }

        var now = DateTimeOffset.UtcNow;
        var units = task.Units.OrderBy(u => u.UnitNo).ToList();
        var available = TaskUnitEngine.UnallocatedUnits(units).Count;
        var quantityStarted = (int)Math.Min(
            available,
            Math.Max(1, Math.Floor(action.QuantityStarted ?? available)));

        var contributors = action.Contributors?.ToList() ?? [];
        if (contributors.Count == 0)
        {
            var operatorId = action.OperatorId ?? action.AssignedTo ?? actor.UserId;
            var operatorName = action.OperatorName ?? action.AssignedToName ?? actor.UserName;
            contributors.Add(new TaskContributorInputDto(
                operatorId, operatorName, null, quantityStarted, null, null, null, null, null, null, null, null));
        }

        var perPerson = SplitQuantity(quantityStarted, contributors.Count);
        var workers = contributors
            .Select((person, index) => (
                person.UserId,
                person.UserName,
                Quantity: person.Quantity.HasValue
                    ? (int)Math.Max(0, Math.Floor(person.Quantity.Value))
                    : perPerson[index]))
            .ToList();

        TaskUnitEngine.AllocateUnitsToWorkers(units, workers, now);
        if (!string.IsNullOrWhiteSpace(action.MachineName))
        {
            // Machine name stored on task entity via reflection not available — skip unless domain supports it
        }

        task.SetNotes(action.Notes ?? task.Notes);
        task.SetStatus(ManufacturingTaskStatuses.InProgress, now);
        TaskUnitEngine.ApplyUnitsDerivedFields(task);
        ManufacturingJobHelper.AddHistory(task, actor, "started", task.Status, ManufacturingTaskStatuses.InProgress, action.Notes);
        job.MarkStarted(now);
    }

    private static void ApplyPause(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.Status != ManufacturingTaskStatuses.InProgress)
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" is not in progress.");
        }

        var now = DateTimeOffset.UtcNow;
        var units = task.Units.ToList();
        TaskUnitEngine.SetWorkerAssignmentStatus(units, null, TaskUnitAssignmentStatuses.Paused, now);
        task.SetStatus(ManufacturingTaskStatuses.Paused, now);
        TaskUnitEngine.ApplyUnitsDerivedFields(task);
        ManufacturingJobHelper.AddHistory(task, actor, "paused", ManufacturingTaskStatuses.InProgress, ManufacturingTaskStatuses.Paused, action.Notes);
    }

    private static void ApplyResume(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.Status is not ManufacturingTaskStatuses.OnHold
            and not ManufacturingTaskStatuses.Ready
            and not ManufacturingTaskStatuses.Blocked
            and not ManufacturingTaskStatuses.Paused)
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" cannot be resumed.");
        }

        var now = DateTimeOffset.UtcNow;
        TaskUnitEngine.SetWorkerAssignmentStatus(task.Units.ToList(), null, TaskUnitAssignmentStatuses.InProgress, now);
        task.SetStatus(ManufacturingTaskStatuses.InProgress, now);
        TaskUnitEngine.ApplyUnitsDerivedFields(task);
        ManufacturingJobHelper.AddHistory(task, actor, "resumed", task.Status, ManufacturingTaskStatuses.InProgress, action.Notes);
        if (job.Status == ManufacturingJobStatuses.OnHold)
        {
            job.SetStatus(ManufacturingJobStatuses.InProgress);
        }
    }

    private static void ApplyHold(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.Status is ManufacturingTaskStatuses.Completed or ManufacturingTaskStatuses.Skipped or ManufacturingTaskStatuses.Cancelled)
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" cannot be put on hold.");
        }

        var now = DateTimeOffset.UtcNow;
        task.SetStatus(ManufacturingTaskStatuses.OnHold, now);
        ManufacturingJobHelper.AddHistory(task, actor, "on_hold", task.Status, ManufacturingTaskStatuses.OnHold, action.Notes);
    }

    private static void ApplyComplete(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.Status is not ManufacturingTaskStatuses.InProgress
            and not ManufacturingTaskStatuses.Ready
            and not ManufacturingTaskStatuses.Paused
            and not ManufacturingTaskStatuses.ReworkRequired)
        {
            ManufacturingErrors.InvalidState($"Task \"{task.Name}\" must be in progress before it can be completed.");
        }

        var units = task.Units.OrderBy(u => u.UnitNo).ToList();
        var remainingBefore = ManufacturingJobHelper.RemainingQuantity(task);
        var availableOrOwned = TaskUnitEngine.UnallocatedUnits(units).Count +
            units.Count(u => u.Assignments.Count > 0 && u.ProgressPercentage < 100);

        var contributorInputs = action.Contributors?.ToList() ?? [];
        if (contributorInputs.Count == 0)
        {
            contributorInputs.Add(new TaskContributorInputDto(
                actor.UserId,
                actor.UserName,
                100,
                remainingBefore,
                action.RejectedQuantity,
                action.WasteQuantity,
                action.ActualHours,
                action.OvertimeHours,
                action.NormalOvertimeHours,
                action.DoubleOvertimeHours,
                100,
                null));
        }

        var peopleQuantity = contributorInputs.Sum(p => p.Quantity ?? 0);
        var finishingTask = peopleQuantity >= remainingBefore && remainingBefore > 0;
        TaskUnitEngine.ValidateUnitContributorInputs(contributorInputs, Math.Max(availableOrOwned, remainingBefore), finishingTask);

        var hoursPerUnit = task.Units.Count > 0 ? task.EstimatedHours / task.Units.Count : task.EstimatedHours;
        TaskUnitEngine.UpdateAssignedUnitProgress(
            units,
            contributorInputs,
            DateTimeOffset.UtcNow,
            hoursPerUnit,
            task.LabourCostRate);

        if (action.ReworkQuantity is > 0)
        {
            task.AddRejectedWaste(0, 0, action.ReworkQuantity.Value);
        }

        task.SetNotes(action.Notes ?? task.Notes);
        TaskUnitEngine.ApplyUnitsDerivedFields(task);
        var allDone = task.Units.Count > 0 && task.Units.All(u => u.IsComplete || u.IsCancelled);
        var newStatus = allDone ? ManufacturingTaskStatuses.Completed : ManufacturingTaskStatuses.InProgress;
        task.SetStatus(newStatus, DateTimeOffset.UtcNow);
        ManufacturingJobHelper.AddHistory(
            task,
            actor,
            allDone ? "completed" : "quantity_updated",
            task.Status,
            newStatus,
            action.Notes);
    }

    private static void ApplySkip(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        if (task.IsRequired)
        {
            ManufacturingErrors.InvalidState($"Required task \"{task.Name}\" cannot be skipped.");
        }

        task.SetStatus(ManufacturingTaskStatuses.Skipped, DateTimeOffset.UtcNow);
        ManufacturingJobHelper.AddHistory(task, actor, "skipped", task.Status, ManufacturingTaskStatuses.Skipped, action.Notes);
    }

    private static void ApplyBlock(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        task.SetStatus(ManufacturingTaskStatuses.Blocked, DateTimeOffset.UtcNow);
        ManufacturingJobHelper.AddHistory(task, actor, "blocked", task.Status, ManufacturingTaskStatuses.Blocked, action.Notes);
    }

    private static void ApplyNotes(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var task = RequireTask(job, action.TaskId.Value);
        task.SetNotes(action.Notes);
        ManufacturingJobHelper.AddHistory(task, actor, "notes_added", task.Status, task.Status, action.Notes);
        job.Touch();
    }

    private static void ApplyRework(ManufacturingJob job, ManufacturingTaskActionDto action, TaskActionActorDto actor)
    {
        if (!action.TaskId.HasValue) ManufacturingErrors.InvalidState("Task id is required.");
        var original = RequireTask(job, action.TaskId.Value);
        var quantity = (int)Math.Max(1, Math.Floor(action.Quantity ?? job.Quantity));
        var reworkTask = ManufacturingTask.Create(
            job.Id,
            $"TASK-{job.Tasks.Count + 1:000}",
            $"Rework: {original.Name}",
            original.Sequence,
            original.EstimatedHours,
            quantity,
            true,
            true,
            false,
            original.ProductOperationId,
            action.Reason,
            original.LabourCostRate,
            original.MachineName,
            original.MachineCost,
            original.OperationSnapshotJson,
            original.GetPrerequisiteTaskIds());
        reworkTask.SetStatus(ManufacturingTaskStatuses.Ready, DateTimeOffset.UtcNow);
        job.Tasks.Add(reworkTask);
        job.SetStatus(ManufacturingJobStatuses.Rework);
        job.Touch();
    }

    private static void ApplyQc(
        ManufacturingJob job,
        ManufacturingTaskActionDto action,
        TaskActionActorDto actor,
        QualityInspectionDto? currentInspection)
    {
        if (action.Inspection is null)
        {
            ManufacturingErrors.InvalidState("QC inspection payload is required.");
        }

        job.SetQualityInspectionJson(JsonColumn.Serialize(action.Inspection));
        if (action.Result == "passed")
        {
            job.SetStatus(ManufacturingJobStatuses.QualityCheck);
        }
        else if (action.Result is "failed" or "rework")
        {
            job.SetStatus(ManufacturingJobStatuses.Rework);
        }

        job.Touch();
    }

    private static IReadOnlyList<int> SplitQuantity(int total, int count)
    {
        if (count <= 0) return [];
        var baseQty = total / count;
        var result = Enumerable.Repeat(baseQty, count).ToArray();
        result[0] += total - baseQty * count;
        return result;
    }
}
