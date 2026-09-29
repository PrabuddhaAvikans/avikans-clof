using Manufacturing.Application.Jobs;
using Manufacturing.Application.Services;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;

namespace Manufacturing.Application.Common;

public static class ManufacturingMappers
{
    public static ManufacturingJobDto MapJob(ManufacturingJob job)
    {
        var inspection = JsonColumn.Deserialize<QualityInspectionDto?>(job.QualityInspectionJson, null);
        var reworks = JsonColumn.Deserialize<List<ManufacturingReworkDto>>(job.ReworksJson, []);
        var materials = JsonColumn.Deserialize<List<MaterialRequirementDto>>(job.MaterialRequirementsJson, []);
        var outcome = JsonColumn.Deserialize<ProductionMaterialOutcomeDto?>(job.CompletionOutcomeJson, null);
        var tasks = job.Tasks.OrderBy(t => t.Sequence).Select(t => MapTask(t, job.Id)).ToList();

        return new ManufacturingJobDto(
            job.Id,
            job.Number,
            job.SalesOrderId,
            job.SalesOrderNumber,
            job.CustomerId,
            job.CustomerName,
            job.ProductId,
            job.ProductSku,
            job.ProductName,
            job.ProductVersionId,
            job.ProductVersionLabel,
            job.Quantity,
            job.Status,
            job.Priority,
            tasks,
            reworks,
            materials,
            inspection,
            job.PlannedStart,
            job.PlannedEnd,
            job.ActualStartUtc,
            job.ActualEndUtc,
            job.OverallProgress,
            job.EstimatedCost,
            job.ActualCost,
            job.AssignedToUserId,
            job.AssignedToName,
            job.Notes,
            outcome,
            job.CreatedBy,
            job.CreatedByName,
            job.CreatedOnUtc,
            job.ModifiedOnUtc);
    }

    public static ManufacturingTaskDto MapTask(ManufacturingTask task, Guid jobId)
    {
        var domainUnits = task.Units.OrderBy(u => u.UnitNo).ToList();
        var units = domainUnits.Select(MapUnit).ToList();
        var contributors = BuildContributors(units);
        var planned = domainUnits.Count;
        var completed = TaskUnitEngine.CompletedUnitCount(domainUnits);
        var partial = TaskUnitEngine.PartiallyCompletedUnitCount(domainUnits);
        var overall = TaskUnitEngine.OverallUnitProgress(domainUnits);
        var workstation = JsonColumn.Deserialize<ProductOperationJson?>(task.OperationSnapshotJson, null)?.Workstation;

        return new ManufacturingTaskDto(
            task.Id,
            task.TaskNumber,
            jobId,
            task.ProductOperationId,
            task.Sequence,
            task.Name,
            task.Description,
            task.IsRequired,
            task.IsEnabled,
            task.IsQcTask,
            task.IsTestingTask,
            task.IsRework,
            task.OriginalTaskId,
            task.EstimatedHours,
            task.ActualHours,
            contributors.Sum(c => c.OvertimeHours),
            contributors.Sum(c => c.NormalOvertimeHours),
            contributors.Sum(c => c.DoubleOvertimeHours),
            task.LabourCostRate,
            task.MachineName,
            task.MachineCost,
            null,
            task.AssignedToUserId,
            task.AssignedToName,
            contributors.FirstOrDefault()?.UserId,
            contributors.FirstOrDefault()?.UserName,
            contributors,
            units,
            planned,
            completed,
            partial,
            task.RejectedQuantity,
            task.ReworkQuantity,
            task.WasteQuantity,
            TaskUnitEngine.AllocatedQuantity(domainUnits),
            overall,
            task.Status,
            task.StartedAtUtc,
            task.CompletedAtUtc,
            task.PausedAtUtc,
            task.Notes,
            task.GetPrerequisiteTaskIds(),
            task.History.OrderBy(h => h.OccurredAtUtc).Select(MapHistory).ToList(),
            [],
            workstation);
    }

    private static TaskUnitDto MapUnit(TaskUnit unit) =>
        new(
            unit.Id,
            unit.TaskId,
            unit.UnitNo,
            unit.ProgressPercentage,
            unit.Status,
            unit.Assignments.Select(MapAssignment).ToList());

    private static TaskUnitAssignmentDto MapAssignment(TaskUnitAssignment assignment) =>
        new(
            assignment.Id,
            assignment.TaskUnitId,
            assignment.UserId,
            assignment.UserName,
            assignment.ContributionPercentage,
            assignment.Status,
            assignment.ActualHours,
            assignment.OvertimeHours,
            assignment.NormalOvertimeHours,
            assignment.DoubleOvertimeHours,
            assignment.LaborCost,
            assignment.RejectedQuantity,
            assignment.WasteQuantity,
            assignment.StartedAtUtc,
            assignment.PausedAtUtc,
            assignment.CompletedAtUtc);

    private static ManufacturingTaskHistoryEntryDto MapHistory(TaskHistoryEntry entry) =>
        new(
            entry.Id,
            entry.TaskId,
            entry.UserId,
            entry.UserName,
            entry.OccurredAtUtc,
            entry.Action,
            entry.OldStatus,
            entry.NewStatus,
            entry.Comments);

    private static IReadOnlyList<TaskContributorDto> BuildContributors(IReadOnlyList<TaskUnitDto> units)
    {
        var byUser = new Dictionary<Guid, TaskContributorDto>();
        foreach (var unit in units)
        {
            foreach (var assignment in unit.Assignments)
            {
                var unitComplete = unit.ProgressPercentage >= 100;
                if (!byUser.TryGetValue(assignment.UserId, out var existing))
                {
                    byUser[assignment.UserId] = new TaskContributorDto(
                        assignment.UserId,
                        assignment.UserName,
                        assignment.ContributionPercentage,
                        1,
                        assignment.RejectedQuantity,
                        assignment.WasteQuantity,
                        assignment.ActualHours,
                        assignment.OvertimeHours,
                        assignment.NormalOvertimeHours,
                        assignment.DoubleOvertimeHours,
                        assignment.LaborCost,
                        assignment.Status,
                        assignment.StartedAt,
                        assignment.PausedAt,
                        assignment.CompletedAt,
                        unit.ProgressPercentage,
                        unitComplete ? 1 : 0,
                        unitComplete ? 0 : 1);
                    continue;
                }

                byUser[assignment.UserId] = existing with
                {
                    Quantity = existing.Quantity + 1,
                    CompletedQuantity = (existing.CompletedQuantity ?? 0) + (unitComplete ? 1 : 0),
                    InProgressQuantity = (existing.InProgressQuantity ?? 0) + (unitComplete ? 0 : 1),
                    ProgressPercentage = Math.Round((existing.ProgressPercentage ?? 0) + unit.ProgressPercentage, 2),
                    ActualHours = existing.ActualHours + assignment.ActualHours,
                    OvertimeHours = existing.OvertimeHours + assignment.OvertimeHours,
                    NormalOvertimeHours = existing.NormalOvertimeHours + assignment.NormalOvertimeHours,
                    DoubleOvertimeHours = existing.DoubleOvertimeHours + assignment.DoubleOvertimeHours,
                    LaborCost = existing.LaborCost + assignment.LaborCost,
                    RejectedQuantity = existing.RejectedQuantity + assignment.RejectedQuantity,
                    WasteQuantity = existing.WasteQuantity + assignment.WasteQuantity,
                };
            }
        }

        return byUser.Values
            .Select(c => c with
            {
                ProgressPercentage = c.Quantity > 0
                    ? Math.Round((c.ProgressPercentage ?? 0) / c.Quantity, 2)
                    : c.ProgressPercentage,
                ContributionPercent = c.Quantity > 0
                    ? Math.Round(c.ContributionPercent / c.Quantity, 2)
                    : c.ContributionPercent,
            })
            .ToList();
    }
}
