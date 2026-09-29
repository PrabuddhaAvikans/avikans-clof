using Manufacturing.Application.Common;
using Manufacturing.Application.Jobs;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;

namespace Manufacturing.Application.Services;

internal static class ManufacturingJobHelper
{
    public static int RemainingQuantity(ManufacturingTask task)
    {
        var units = task.Units.OrderBy(u => u.UnitNo).ToList();
        if (units.Count > 0)
        {
            return Math.Max(0, units.Count - TaskUnitEngine.CompletedUnitCount(units));
        }

        return 0;
    }

    public static bool ArePrerequisitesMet(ManufacturingTask task, IReadOnlyList<ManufacturingTask> tasks)
    {
        var prereqIds = task.GetPrerequisiteTaskIds();
        if (prereqIds.Count == 0) return true;
        return prereqIds.All(prereqId =>
        {
            var prereq = tasks.FirstOrDefault(t => t.Id == prereqId);
            if (prereq is null) return true;
            if (!prereq.IsRequired)
            {
                return ManufacturingTaskStatuses.SatisfiedPrereq.Contains(prereq.Status);
            }

            return prereq.Status == ManufacturingTaskStatuses.Completed;
        });
    }

    public static void ApplyTaskReadiness(ManufacturingJob job, TaskActionActorDto? actor)
    {
        var tasks = job.Tasks.OrderBy(t => t.Sequence).ToList();
        foreach (var task in tasks)
        {
            if (task.Status is not ManufacturingTaskStatuses.Pending and not ManufacturingTaskStatuses.Blocked)
            {
                continue;
            }

            if (!ArePrerequisitesMet(task, tasks))
            {
                if (task.Status == ManufacturingTaskStatuses.Blocked)
                {
                    task.SetStatus(ManufacturingTaskStatuses.Pending, DateTimeOffset.UtcNow);
                }

                continue;
            }

            if (actor is null)
            {
                task.SetStatus(ManufacturingTaskStatuses.Ready, DateTimeOffset.UtcNow);
                continue;
            }

            AddHistory(task, actor, "ready", task.Status, ManufacturingTaskStatuses.Ready, "Prerequisites satisfied");
            task.SetStatus(ManufacturingTaskStatuses.Ready, DateTimeOffset.UtcNow);
        }
    }

    public static bool IsQcPassed(ManufacturingJob job, QualityInspectionDto? inspection)
    {
        var qcTasks = job.Tasks.Where(t => t.IsQcTask && t.IsRequired && t.IsEnabled).ToList();
        if (qcTasks.Count > 0)
        {
            if (!qcTasks.All(t => t.Status == ManufacturingTaskStatuses.Completed))
            {
                return false;
            }

            if (inspection?.Status is "failed" or "rework")
            {
                return false;
            }

            return true;
        }

        if (inspection is not null)
        {
            return inspection.Status == "passed";
        }

        return true;
    }

    public static bool IsProductionJobCompletable(ManufacturingJob job, QualityInspectionDto? inspection)
    {
        if (job.Status == ManufacturingJobStatuses.Cancelled) return false;
        var required = job.Tasks
            .Where(t => t.IsEnabled && t.IsRequired && t.Status != ManufacturingTaskStatuses.Cancelled)
            .ToList();
        if (required.Count == 0) return false;
        if (!required.All(t => t.Status == ManufacturingTaskStatuses.Completed))
        {
            return false;
        }

        return IsQcPassed(job, inspection);
    }

    public static decimal CalculateTaskProgressPercent(IReadOnlyList<ManufacturingTask> tasks)
    {
        var tracked = tasks
            .Where(t => t.IsEnabled && t.IsRequired && !t.IsRework)
            .ToList();
        if (tracked.Count == 0)
        {
            tracked = tasks.Where(t => t.IsEnabled).ToList();
        }

        if (tracked.Count == 0) return 0;

        decimal totalQuantity = 0;
        decimal earned = 0;
        foreach (var task in tracked)
        {
            var units = task.Units.ToList();
            var planned = units.Count;
            totalQuantity += planned;
            if (planned == 0) continue;
            earned += units.Sum(u => u.ProgressPercentage / 100m);
        }

        if (totalQuantity <= 0)
        {
            var completed = tracked.Count(t =>
                t.Status is ManufacturingTaskStatuses.Completed or ManufacturingTaskStatuses.Skipped);
            return Math.Round(completed * 100m / tracked.Count, 2, MidpointRounding.AwayFromZero);
        }

        return Math.Round(earned / totalQuantity * 100m, 2, MidpointRounding.AwayFromZero);
    }

    public static void RefreshJobDerivedFields(ManufacturingJob job, QualityInspectionDto? inspection)
    {
        foreach (var task in job.Tasks)
        {
            TaskUnitEngine.ApplyUnitsDerivedFields(task);
        }

        ApplyTaskReadiness(job, actor: null);
        job.SetOverallProgress(CalculateTaskProgressPercent(job.Tasks.ToList()));

        if (IsProductionJobCompletable(job, inspection) &&
            job.Status is not ManufacturingJobStatuses.Completed
            and not ManufacturingJobStatuses.Cancelled
            and not ManufacturingJobStatuses.Draft)
        {
            // Completing the job is explicit via CompleteJobAsync.
        }
    }

    public static ManufacturingTask? CurrentTask(ManufacturingJob job) =>
        job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.InProgress)
        ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.Paused)
        ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.Ready)
        ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.ReworkRequired);

    public static void AddHistory(
        ManufacturingTask task,
        TaskActionActorDto actor,
        string action,
        string? oldStatus,
        string? newStatus,
        string? comments)
    {
        task.AddHistory(TaskHistoryEntry.Create(
            task.Id,
            actor.UserId,
            actor.UserName,
            action,
            oldStatus,
            newStatus,
            comments));
    }

    public static bool IsQcOperation(string name, bool? isQualityCheck) =>
        isQualityCheck == true || name.Contains("qc", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("quality", StringComparison.OrdinalIgnoreCase);

    public static bool IsTestingOperation(string name) =>
        name.Contains("test", StringComparison.OrdinalIgnoreCase);
}
