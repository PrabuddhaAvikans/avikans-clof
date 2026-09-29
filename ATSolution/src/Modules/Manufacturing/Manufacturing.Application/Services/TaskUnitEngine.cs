using Manufacturing.Application.Common;
using Manufacturing.Application.Jobs;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Tasks;

namespace Manufacturing.Application.Services;

internal static class TaskUnitEngine
{
    private const decimal Complete = 100m;
    private const decimal Tolerance = 0.05m;

    public static decimal ClampProgress(decimal value) =>
        Math.Round(Math.Clamp(value, 0, Complete), 2, MidpointRounding.AwayFromZero);

    public static int CompletedUnitCount(IEnumerable<TaskUnit> units) =>
        units.Count(u => u.IsComplete || u.ProgressPercentage >= Complete);

    public static int PartiallyCompletedUnitCount(IEnumerable<TaskUnit> units) =>
        units.Count(u => u.ProgressPercentage > 0 && u.ProgressPercentage < Complete);

    public static decimal OverallUnitProgress(IReadOnlyList<TaskUnit> units)
    {
        if (units.Count == 0) return 0;
        return ClampProgress(units.Average(u => u.ProgressPercentage));
    }

    public static int AllocatedQuantity(IEnumerable<TaskUnit> units) =>
        units.Count(u => u.Assignments.Count > 0 || u.Status != TaskUnitStatuses.Pending);

    public static IReadOnlyList<TaskUnit> UnallocatedUnits(IReadOnlyList<TaskUnit> units) =>
        units.Where(u => u.Assignments.Count == 0 && u.Status == TaskUnitStatuses.Pending).ToList();

    public static void AllocateUnitsToWorkers(
        IReadOnlyList<TaskUnit> units,
        IReadOnlyList<(Guid UserId, string UserName, int Quantity)> workers,
        DateTimeOffset now)
    {
        var pool = UnallocatedUnits(units).Select(u => u.UnitNo).ToList();
        var requested = workers.Sum(w => Math.Max(0, w.Quantity));
        if (requested > pool.Count)
        {
            ManufacturingErrors.InvalidState(
                $"Only {pool.Count} quantit{(pool.Count == 1 ? "y" : "ies")} available to assign (requested {requested}).");
        }

        foreach (var worker in workers)
        {
            var qty = Math.Max(0, worker.Quantity);
            if (qty <= 0) continue;
            var take = pool.Take(qty).ToList();
            pool = pool.Skip(qty).ToList();
            foreach (var unitNo in take)
            {
                var unit = units.First(u => u.UnitNo == unitNo);
                unit.Assignments.Clear();
                unit.Assignments.Add(TaskUnitAssignment.Create(
                    unit.Id,
                    worker.UserId,
                    worker.UserName,
                    Complete,
                    TaskUnitAssignmentStatuses.InProgress,
                    now));
                unit.SetProgress(Math.Max(unit.ProgressPercentage, 0));
                unit.SyncStatusFromProgress();
            }
        }
    }

    public static void AssignSharedUnits(
        IReadOnlyList<TaskUnit> units,
        IReadOnlyList<int> unitNos,
        IReadOnlyList<TaskContributorInputDto> workers,
        decimal progressPercentage,
        DateTimeOffset now,
        decimal hoursPerUnit,
        decimal? labourCostRate)
    {
        if (unitNos.Count == 0) return;
        var progress = ClampProgress(progressPercentage);
        if (progress >= Complete && !HasCompleteContribution(workers))
        {
            var sum = workers.Sum(w => w.ContributionPercent ?? 0);
            ManufacturingErrors.InvalidState(
                $"Worker shares must add up to 100% to complete shared quantity (currently {sum:0.##}%).");
        }

        foreach (var unit in units.Where(u => unitNos.Contains(u.UnitNo)))
        {
            unit.Assignments.Clear();
            foreach (var worker in workers)
            {
                var share = worker.ContributionPercent ?? Complete;
                var estimatedHours = Math.Round(hoursPerUnit * (share / 100m), 2, MidpointRounding.AwayFromZero);
                var actualHours = worker.ActualHours ?? estimatedHours;
                var laborCost = labourCostRate.HasValue
                    ? Math.Round(actualHours * labourCostRate.Value, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var assignment = TaskUnitAssignment.Create(
                    unit.Id,
                    worker.UserId,
                    worker.UserName,
                    share,
                    AssignmentStatusFromProgress(progress),
                    now);
                assignment.ApplyProgress(
                    share,
                    AssignmentStatusFromProgress(progress),
                    actualHours,
                    worker.NormalOvertimeHours ?? 0,
                    worker.DoubleOvertimeHours ?? 0,
                    laborCost,
                    worker.RejectedQuantity ?? 0,
                    worker.WasteQuantity ?? 0,
                    now);
                unit.Assignments.Add(assignment);
            }

            unit.SetProgress(progress);
        }
    }

    public static void UpdateAssignedUnitProgress(
        IReadOnlyList<TaskUnit> units,
        IReadOnlyList<TaskContributorInputDto> updates,
        DateTimeOffset now,
        decimal hoursPerUnit,
        decimal? labourCostRate)
    {
        var sharedGroups = updates
            .Where(u => u.UnitNos is { Count: > 0 })
            .GroupBy(u => string.Join(",", u.UnitNos!.OrderBy(x => x)))
            .ToList();
        var solo = updates.Where(u => u.UnitNos is null or { Count: 0 }).ToList();

        foreach (var group in sharedGroups)
        {
            var unitNos = group.First().UnitNos!.ToList();
            var progress = group.Max(x => x.ProgressPercentage ?? Complete);
            AssignSharedUnits(units, unitNos, group.ToList(), progress, now, hoursPerUnit, labourCostRate);
        }

        foreach (var update in solo)
        {
            var qty = (int)Math.Max(0, Math.Floor(update.Quantity ?? 0));
            if (qty <= 0) continue;
            var progress = ClampProgress(update.ProgressPercentage ?? Complete);

            var owned = units
                .Where(u =>
                    u.Assignments.Any(a => a.UserId == update.UserId) &&
                    u.ProgressPercentage < Complete)
                .OrderBy(u => u.ProgressPercentage)
                .ThenBy(u => u.UnitNo)
                .ToList();
            var free = UnallocatedUnits(units);
            var targets = owned.Concat(free).Take(qty).ToList();
            if (targets.Count < qty)
            {
                ManufacturingErrors.InvalidState(
                    $"Only {targets.Count} quantit{(targets.Count == 1 ? "y" : "ies")} available for {update.UserName} (requested {qty}).");
            }

            foreach (var unit in targets)
            {
                var share = update.ContributionPercent ?? Complete;
                var estimatedHours = Math.Round(hoursPerUnit * (share / 100m), 2, MidpointRounding.AwayFromZero);
                var hoursEach = update.ActualHours.HasValue && qty > 0
                    ? update.ActualHours.Value / qty
                    : estimatedHours;
                var laborCost = labourCostRate.HasValue
                    ? Math.Round(hoursEach * labourCostRate.Value, 2, MidpointRounding.AwayFromZero)
                    : 0m;
                var existing = unit.Assignments.FirstOrDefault(a => a.UserId == update.UserId);
                if (existing is not null)
                {
                    unit.Assignments.Remove(existing);
                }

                var assignment = TaskUnitAssignment.Create(
                    unit.Id,
                    update.UserId,
                    update.UserName,
                    share,
                    AssignmentStatusFromProgress(progress, existing?.Status),
                    existing?.StartedAtUtc ?? now);
                assignment.ApplyProgress(
                    share,
                    AssignmentStatusFromProgress(progress, existing?.Status),
                    hoursEach,
                    (update.NormalOvertimeHours ?? 0) / Math.Max(1, qty),
                    (update.DoubleOvertimeHours ?? 0) / Math.Max(1, qty),
                    laborCost,
                    (update.RejectedQuantity ?? 0) / Math.Max(1, qty),
                    (update.WasteQuantity ?? 0) / Math.Max(1, qty),
                    now);
                unit.Assignments.Add(assignment);
                unit.SetProgress(progress);
            }
        }
    }

    public static void SetWorkerAssignmentStatus(
        IReadOnlyList<TaskUnit> units,
        Guid? employeeId,
        string status,
        DateTimeOffset at)
    {
        foreach (var unit in units)
        {
            if (unit.ProgressPercentage >= Complete) continue;
            foreach (var assignment in unit.Assignments)
            {
                if (employeeId.HasValue && assignment.UserId != employeeId.Value) continue;
                if (assignment.Status == TaskUnitAssignmentStatuses.Completed) continue;
                assignment.SetStatus(status, at);
            }

            unit.SyncStatusFromProgress();
        }
    }

    public static void ApplyUnitsDerivedFields(ManufacturingTask task)
    {
        var units = task.Units.OrderBy(u => u.UnitNo).ToList();
        foreach (var unit in units)
        {
            unit.SyncStatusFromProgress();
        }

        var completed = CompletedUnitCount(units);
        var allComplete = units.Count > 0 && completed == units.Count;
        var actualHours = units
            .SelectMany(u => u.Assignments)
            .Sum(a => a.ActualHours);
        task.SetActualHours(Math.Round(actualHours, 2, MidpointRounding.AwayFromZero));

        if (allComplete && task.Status is not ManufacturingTaskStatuses.Skipped
            and not ManufacturingTaskStatuses.Cancelled
            and not ManufacturingTaskStatuses.Blocked)
        {
            task.SetStatus(ManufacturingTaskStatuses.Completed, DateTimeOffset.UtcNow);
        }
        else if (
            task.Status == ManufacturingTaskStatuses.Completed &&
            !allComplete)
        {
            task.SetStatus(ManufacturingTaskStatuses.InProgress, DateTimeOffset.UtcNow);
        }
        else if (
            AllocatedQuantity(units) > 0 &&
            task.Status is ManufacturingTaskStatuses.Ready or ManufacturingTaskStatuses.Pending)
        {
            task.SetStatus(ManufacturingTaskStatuses.InProgress, DateTimeOffset.UtcNow);
        }

        var primary = units
            .SelectMany(u => u.Assignments)
            .FirstOrDefault();
        if (primary is not null)
        {
            task.SetAssignees(primary.UserId, primary.UserName);
        }
    }

    public static void ValidateUnitContributorInputs(
        IReadOnlyList<TaskContributorInputDto> inputs,
        int availableOrOwned,
        bool finishing)
    {
        var people = inputs.Where(p => p.UserId != Guid.Empty).ToList();
        if (people.Count == 0)
        {
            ManufacturingErrors.InvalidState("Assign at least one person before updating this task.");
        }

        var qtyTotal = people.Sum(p => p.Quantity ?? 0);
        if (qtyTotal - availableOrOwned > Tolerance)
        {
            ManufacturingErrors.InvalidState(
                $"Person quantities must not exceed available {availableOrOwned} (currently {qtyTotal:0.##}).");
        }

        var attemptingComplete = people.Any(p =>
        {
            var progress = p.ProgressPercentage ?? (finishing ? Complete : 0);
            return progress >= Complete - Tolerance;
        });
        if (!attemptingComplete) return;

        var sharedGroups = people
            .Where(p => p.UnitNos is { Count: > 0 })
            .GroupBy(p => string.Join(",", p.UnitNos!.OrderBy(x => x)));
        foreach (var group in sharedGroups)
        {
            if (!HasCompleteContribution(group))
            {
                var sum = group.Sum(p => p.ContributionPercent ?? 0);
                ManufacturingErrors.InvalidState(
                    $"Worker shares must add up to 100% to complete shared quantity (currently {sum:0.##}%).");
            }
        }
    }

    private static bool HasCompleteContribution(IEnumerable<TaskContributorInputDto> workers)
    {
        var sum = workers.Sum(w => w.ContributionPercent ?? 0);
        return Math.Abs(sum - Complete) <= Tolerance;
    }

    private static string AssignmentStatusFromProgress(decimal progress, string? previous = null)
    {
        if (progress >= Complete) return TaskUnitAssignmentStatuses.Completed;
        if (progress > 0) return TaskUnitAssignmentStatuses.InProgress;
        return previous ?? TaskUnitAssignmentStatuses.Assigned;
    }
}
