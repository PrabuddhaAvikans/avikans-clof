using ATSolution.Application.Abstractions.Persistence;
using ATSolution.SharedKernel.Models;
using Manufacturing.Application.Abstractions;
using Manufacturing.Application.Common;
using Manufacturing.Application.Jobs;
using Manufacturing.Application.ProductionTracking;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Microsoft.EntityFrameworkCore;

namespace Manufacturing.Application.Services;

public sealed class ProductionTrackingService : IProductionTrackingService
{
    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IManufacturingService _manufacturingService;

    public ProductionTrackingService(
        IRepository<ManufacturingJob, Guid> jobs,
        IManufacturingService manufacturingService)
    {
        _jobs = jobs;
        _manufacturingService = manufacturingService;
    }

    public async Task<ProductionTrackingSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        var jobs = await _jobs.Query()
            .AsNoTracking()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Units)
            .ThenInclude(u => u.Assignments)
            .ToListAsync(cancellationToken);

        var views = jobs.Select(ToProductionJob).ToList();
        var inProduction = views.Count(j =>
            j.Status is ManufacturingJobStatuses.InProgress
                or ManufacturingJobStatuses.ReadyToStart
                or ManufacturingJobStatuses.QualityCheck
                or ManufacturingJobStatuses.Rework);
        var onHold = views.Count(j => j.Status == ManufacturingJobStatuses.OnHold);
        var inQc = views.Count(j => j.Status == ManufacturingJobStatuses.QualityCheck);
        var delayed = views.Count(j => j.OverdueDays is > 0);

        var kpis = new ProductionKpisDto(
            inProduction, 0,
            onHold, 0,
            inQc, 0,
            delayed, 0,
            views.Count(j => j.Status == ManufacturingJobStatuses.Completed), 0);

        var lines = views.Select(v => v.Line).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var supervisors = views
            .Where(v => v.SupervisorId != Guid.Empty)
            .GroupBy(v => v.SupervisorId)
            .Select(g => new SupervisorOptionDto(g.Key, g.First().SupervisorName))
            .ToList();

        return new ProductionTrackingSnapshotDto(
            kpis,
            views,
            [],
            lines,
            supervisors);
    }

    public async Task<PaginatedResponse<ProductionJobDto>> ListJobsAsync(
        ProductionTrackingFilters query,
        CancellationToken cancellationToken = default)
    {
        var jobs = await _jobs.Query()
            .AsNoTracking()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Units)
            .ThenInclude(u => u.Assignments)
            .ToListAsync(cancellationToken);

        IEnumerable<ProductionJobDto> items = jobs.Select(ToProductionJob);
        if (!string.IsNullOrWhiteSpace(query.Line))
            items = items.Where(j => j.Line.Equals(query.Line, StringComparison.OrdinalIgnoreCase));
        if (query.SupervisorId.HasValue)
            items = items.Where(j => j.SupervisorId == query.SupervisorId);
        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(j => j.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.Priority))
            items = items.Where(j => j.Priority == query.Priority);
        if (query.DelayedOnly == true)
            items = items.Where(j => j.OverdueDays is > 0);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(j =>
                j.JobNumber.Contains(search, StringComparison.OrdinalIgnoreCase)
                || j.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || j.CustomerName.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var list = items.ToList();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var pageItems = list.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return PaginatedResponse<ProductionJobDto>.Create(pageItems, list.Count, page, pageSize);
    }

    public async Task<ProductionJobDto?> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await _jobs.Query()
            .AsNoTracking()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Units)
            .ThenInclude(u => u.Assignments)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

        return job is null ? null : ToProductionJob(job);
    }

    public async Task StartProductionAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default)
    {
        foreach (var id in ids)
        {
            await _manufacturingService.StartJobAsync(id, cancellationToken);
        }
    }

    public async Task<ProductionJobDto> UpdateStageAsync(
        Guid id,
        string? comment,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default)
    {
        var dto = await _manufacturingService.GetByIdAsync(id, cancellationToken)
            ?? throw new ATSolution.Application.NotFoundException($"Manufacturing job '{id}' was not found.");

        var active = dto.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.InProgress)
            ?? dto.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.Ready);

        if (active is null)
        {
            ManufacturingErrors.InvalidState("No manufacturing task is ready to advance.");
        }

        if (active!.Status == ManufacturingTaskStatuses.InProgress)
        {
            var remaining = Math.Max(0, active.PlannedQuantity - active.CompletedQuantity);
            dto = await _manufacturingService.ApplyTaskActionAsync(
                id,
                new ManufacturingTaskActionDto
                {
                    Type = "complete",
                    TaskId = active.Id,
                    CompletedQuantity = remaining,
                    Notes = comment,
                },
                actor,
                cancellationToken);
        }
        else
        {
            dto = await _manufacturingService.ApplyTaskActionAsync(
                id,
                new ManufacturingTaskActionDto { Type = "start", TaskId = active.Id, Notes = comment },
                actor,
                cancellationToken);
        }

        return ToProductionJob(dto);
    }

    public async Task<ProductionJobDto> HoldJobAsync(
        Guid id,
        string? reason,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default)
    {
        var dto = await _manufacturingService.HoldJobAsync(id, reason, cancellationToken);
        return ToProductionJob(dto);
    }

    public async Task<ProductionJobDto> ReleaseToQcAsync(
        Guid id,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default)
    {
        var dto = await _manufacturingService.GetByIdAsync(id, cancellationToken)
            ?? throw new ATSolution.Application.NotFoundException($"Manufacturing job '{id}' was not found.");

        var qcTask = dto.Tasks.FirstOrDefault(t => t.IsQcTask && t.IsEnabled && !t.IsRework);
        if (qcTask is null)
        {
            ManufacturingErrors.InvalidState("This product version has no QC manufacturing task.");
        }

        if (qcTask!.Status == ManufacturingTaskStatuses.Pending)
        {
            ManufacturingErrors.InvalidState("QC is not ready. Complete required prerequisite tasks first.");
        }

        if (qcTask.Status is ManufacturingTaskStatuses.Ready or ManufacturingTaskStatuses.ReworkRequired)
        {
            dto = await _manufacturingService.ApplyTaskActionAsync(
                id,
                new ManufacturingTaskActionDto { Type = "start", TaskId = qcTask.Id },
                actor,
                cancellationToken);
        }

        return ToProductionJob(dto);
    }

    private static ProductionJobDto ToProductionJob(ManufacturingJob job)
    {
        var dto = ManufacturingMappers.MapJob(job);
        return ToProductionJob(dto);
    }

    private static ProductionJobDto ToProductionJob(ManufacturingJobDto job)
    {
        var active = job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.InProgress)
            ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.Paused)
            ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.Ready)
            ?? job.Tasks.FirstOrDefault(t => t.Status == ManufacturingTaskStatuses.ReworkRequired);

        var currentTaskName = active?.Name ?? job.Status;
        var materialIssued = job.MaterialRequirements.Sum(m => m.IssuedQuantity);
        var materialConsumed = job.MaterialRequirements.Sum(m => Math.Min(m.IssuedQuantity, m.RequiredQuantity));
        var laborHours = job.Tasks.Sum(t => t.ActualHours ?? 0);
        var overtimeHours = job.Tasks.Sum(t => t.OvertimeHours ?? 0);
        var normalOt = job.Tasks.Sum(t => t.NormalOvertimeHours ?? 0);
        var doubleOt = job.Tasks.Sum(t => t.DoubleOvertimeHours ?? 0);
        var laborCost = job.Tasks.SelectMany(t => t.Contributors).Sum(c => c.LaborCost);
        var estimatedHours = job.Tasks.Sum(t => t.EstimatedHours);
        var overdueDays = job.Status is not ManufacturingJobStatuses.Completed
            and not ManufacturingJobStatuses.Cancelled
            && job.PlannedEndDate < DateTimeOffset.UtcNow
            ? (int?)Math.Ceiling((DateTimeOffset.UtcNow - job.PlannedEndDate).TotalDays)
            : null;

        var qcOpen = job.QualityInspection is not null && job.QualityInspection.Status != "passed" ? 1 : 0;
        var qcClosed = job.QualityInspection?.Status == "passed" ? 1 : 0;
        var blockers = job.Tasks
            .Where(t => t.Status is ManufacturingTaskStatuses.Blocked or ManufacturingTaskStatuses.OnHold)
            .Select(t => $"{t.Name}: {t.Status.Replace('_', ' ')}")
            .ToList();

        return new ProductionJobDto(
            job.Id,
            job.JobNumber,
            job.SalesOrderNumber,
            job.ProductName,
            job.ProductSku,
            null,
            job.CustomerName,
            job.Quantity,
            job.Priority,
            currentTaskName,
            currentTaskName,
            job.AssignedTo ?? Guid.Empty,
            job.AssignedToName ?? "Unassigned",
            job.PlannedStartDate,
            job.PlannedEndDate,
            job.ProgressPercent,
            job.Status,
            job.Status.Replace('_', ' '),
            job.ActualEndDate,
            job.Tasks.Where(t => !t.IsRework).Select(t => new ProductionStageTaskDto(
                t.Id,
                t.Name,
                t.Sequence,
                ToStageStatus(t.Status),
                t.PlannedQuantity,
                t.CompletedQuantity,
                t.OverallProgress,
                t.CompletedAt)).ToList(),
            materialIssued,
            materialConsumed,
            job.MaterialRequirements.FirstOrDefault()?.Unit ?? "pcs",
            job.MaterialRequirements.Count == 0 ||
            job.MaterialRequirements.All(m =>
                m.Status is MaterialRequirementStatuses.Issued or MaterialRequirementStatuses.Reserved ||
                m.ReservedQuantity >= m.RequiredQuantity),
            blockers,
            laborHours,
            overtimeHours,
            normalOt,
            doubleOt,
            laborCost,
            0,
            job.EstimatedCost,
            job.ActualCost,
            qcOpen,
            qcClosed,
            laborHours,
            Math.Max(0, estimatedHours - laborHours),
            overdueDays);
    }

    private static string ToStageStatus(string status) =>
        status switch
        {
            ManufacturingTaskStatuses.Completed or ManufacturingTaskStatuses.Skipped => "completed",
            ManufacturingTaskStatuses.InProgress => "in_progress",
            ManufacturingTaskStatuses.Paused => "paused",
            ManufacturingTaskStatuses.Ready => "ready",
            _ => "pending",
        };
}
