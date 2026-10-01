using ATSolution.Application.Abstractions.Persistence;
using Delivery.Domain.Common;
using Finance.Domain.Common;
using Finance.Domain.CreditNotes;
using Finance.Domain.Invoices;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;
using Manufacturing.Domain.Work;
using Microsoft.EntityFrameworkCore;
using PeriodClose.Application.Abstractions;
using PeriodClose.Application.PeriodClose;
using Sales.Domain.Common;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;

namespace PeriodClose.Infrastructure.Services;

public sealed class EfPeriodCloseOperations : IPeriodCloseOperations
{
    private const decimal NormalOvertimeMultiplier = 1.5m;
    private const decimal DoubleOvertimeMultiplier = 2m;

    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IRepository<ManufacturingTask, Guid> _tasks;
    private readonly IRepository<TaskUnit, Guid> _units;
    private readonly IRepository<TaskUnitAssignment, Guid> _assignments;
    private readonly IRepository<EmployeeWorkSession, Guid> _sessions;
    private readonly IRepository<SalesOrder, Guid> _orders;
    private readonly IRepository<Quotation, Guid> _quotations;
    private readonly IRepository<Invoice, Guid> _invoices;
    private readonly IRepository<CreditNote, Guid> _creditNotes;
    private readonly IRepository<DeliveryEntity, Guid> _deliveries;
    private readonly IRepository<InventoryItem, Guid> _items;
    private readonly IRepository<StockMovement, Guid> _movements;

    public EfPeriodCloseOperations(
        IRepository<ManufacturingJob, Guid> jobs,
        IRepository<ManufacturingTask, Guid> tasks,
        IRepository<TaskUnit, Guid> units,
        IRepository<TaskUnitAssignment, Guid> assignments,
        IRepository<EmployeeWorkSession, Guid> sessions,
        IRepository<SalesOrder, Guid> orders,
        IRepository<Quotation, Guid> quotations,
        IRepository<Invoice, Guid> invoices,
        IRepository<CreditNote, Guid> creditNotes,
        IRepository<DeliveryEntity, Guid> deliveries,
        IRepository<InventoryItem, Guid> items,
        IRepository<StockMovement, Guid> movements)
    {
        _jobs = jobs;
        _tasks = tasks;
        _units = units;
        _assignments = assignments;
        _sessions = sessions;
        _orders = orders;
        _quotations = quotations;
        _invoices = invoices;
        _creditNotes = creditNotes;
        _deliveries = deliveries;
        _items = items;
        _movements = movements;
    }

    public async Task<DayOperationalPicture> LoadDayAsync(
        string businessDate,
        PeriodClosePolicy policy,
        CancellationToken cancellationToken = default)
    {
        var (start, end) = DayRange(businessDate);
        var day = DateOnly.Parse(businessDate);

        var jobs = await _jobs.Query().AsNoTracking()
            .Where(j => j.Status != ManufacturingJobStatuses.Cancelled)
            .ToListAsync(cancellationToken);
        var jobIds = jobs.Select(j => j.Id).ToList();
        var tasks = jobIds.Count == 0
            ? []
            : await _tasks.Query().AsNoTracking().Where(t => jobIds.Contains(t.JobId)).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToList();
        var units = taskIds.Count == 0
            ? []
            : await _units.Query().AsNoTracking().Where(u => taskIds.Contains(u.TaskId)).ToListAsync(cancellationToken);
        var unitIds = units.Select(u => u.Id).ToList();
        var assignments = unitIds.Count == 0
            ? []
            : await _assignments.Query().AsNoTracking().Where(a => unitIds.Contains(a.TaskUnitId)).ToListAsync(cancellationToken);
        var sessions = await _sessions.Query().AsNoTracking()
            .Where(s => s.BusinessDate == day)
            .ToListAsync(cancellationToken);

        var jobsById = jobs.ToDictionary(j => j.Id);
        var tasksById = tasks.ToDictionary(t => t.Id);
        var unitsByTask = units.GroupBy(u => u.TaskId).ToDictionary(g => g.Key, g => g.ToList());
        var assignmentsByUnit = assignments.GroupBy(a => a.TaskUnitId).ToDictionary(g => g.Key, g => g.ToList());

        var production = BuildDailyProduction(businessDate, day, jobsById, tasks, unitsByTask, assignmentsByUnit, sessions);
        var employees = BuildEmployees(businessDate, day, policy, jobsById, tasksById, units, assignments, sessions);
        var activeSessions = BuildActiveSessions(day, jobsById, tasksById, units, assignments, sessions);

        var orders = await _orders.Query().AsNoTracking()
            .Where(o => o.CreatedOnUtc >= start && o.CreatedOnUtc < end)
            .ToListAsync(cancellationToken);
        var quotations = await _quotations.Query().AsNoTracking()
            .Where(q => q.CreatedOnUtc >= start && q.CreatedOnUtc < end)
            .ToListAsync(cancellationToken);
        var invoices = await _invoices.Query().AsNoTracking()
            .Where(i => i.IssueDate >= start && i.IssueDate < end && i.Status != InvoiceStatuses.Void)
            .ToListAsync(cancellationToken);
        var creditNotes = await _creditNotes.Query().AsNoTracking()
            .Where(c => c.CreatedOnUtc >= start && c.CreatedOnUtc < end && c.Status != CreditNoteStatuses.Void)
            .ToListAsync(cancellationToken);
        var deliveries = await _deliveries.Query().AsNoTracking()
            .Where(d => d.DeliveredAtUtc >= start && d.DeliveredAtUtc < end && d.Status == DeliveryStatuses.Delivered)
            .ToListAsync(cancellationToken);
        var movements = await _movements.Query().AsNoTracking()
            .Where(m => m.PerformedAtUtc >= start && m.PerformedAtUtc < end)
            .ToListAsync(cancellationToken);

        var issues = new List<OperationalIssue>();
        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatuses.Draft))
        {
            issues.Add(new OperationalIssue(
                "INVOICE_UNPOSTED",
                "finance",
                $"Invoice {invoice.InvoiceNumber} is not posted for this business date.",
                true,
                "invoice",
                invoice.Id.ToString()));
        }

        var invoicedOrders = invoices
            .Where(i => i.SalesOrderId.HasValue && i.Status != InvoiceStatuses.Draft)
            .Select(i => i.SalesOrderId!.Value)
            .ToHashSet();
        foreach (var delivery in deliveries.Where(d => !invoicedOrders.Contains(d.SalesOrderId)))
        {
            var hasDraft = invoices.Any(i => i.SalesOrderId == delivery.SalesOrderId && i.Status == InvoiceStatuses.Draft);
            if (hasDraft) continue;
            issues.Add(new OperationalIssue(
                "ORDER_DELIVERED_WITHOUT_INVOICE",
                "orders",
                $"Order {delivery.SalesOrderNumber} is marked delivered without a required invoice.",
                true,
                "sales_order",
                delivery.SalesOrderId.ToString()));
        }

        var movementItemIds = movements.Select(m => m.InventoryItemId).Distinct().ToList();
        var movementItems = movementItemIds.Count == 0
            ? new List<InventoryItem>()
            : await _items.Query().AsNoTracking().Where(i => movementItemIds.Contains(i.Id)).ToListAsync(cancellationToken);
        var inventory = BuildDailyInventory(businessDate, movements, movementItems);

        var postedInvoices = invoices.Where(i => i.Status != InvoiceStatuses.Draft).ToList();
        var outstanding = await _invoices.Query().AsNoTracking()
            .Where(i => i.Status != InvoiceStatuses.Void && i.Status != InvoiceStatuses.Draft && i.OutstandingAmount > 0)
            .SumAsync(i => (decimal?)i.OutstandingAmount, cancellationToken) ?? 0;
        var paymentTotal = postedInvoices.Sum(i => i.AmountPaid);
        var figures = new DailyActivityFigures(
            orders.Count(o => o.Status != SalesOrderStatuses.Cancelled),
            production.Select(p => p.ProductionOrderId).Distinct().Count(),
            production.Sum(p => p.CompletedQty),
            production.Sum(p => p.PartialQty),
            postedInvoices.Count,
            postedInvoices.Sum(i => i.TotalAmount),
            postedInvoices.Count(i => i.AmountPaid > 0),
            paymentTotal,
            deliveries.Count,
            movements.Count(m => m.Type == StockMovementTypes.Issue),
            movements.Count(m => m.Type == StockMovementTypes.Receipt),
            movements.Count,
            quotations.Where(q => q.Status is not QuotationStatuses.Rejected and not QuotationStatuses.Expired).Sum(q => q.TotalAmount),
            orders.Where(o => o.Status != SalesOrderStatuses.Cancelled).Sum(o => o.TotalAmount),
            creditNotes.Where(c => c.Status != CreditNoteStatuses.Draft).Sum(c => c.TotalAmount),
            outstanding,
            outstanding,
            outstanding,
            new DailyTransactionRefsDto(
                orders.Select(o => o.Id.ToString()).ToList(),
                postedInvoices.Select(i => i.Id.ToString()).ToList(),
                postedInvoices.Where(i => i.AmountPaid > 0).Select(i => i.Id.ToString()).ToList(),
                deliveries.Select(d => d.Id.ToString()).ToList(),
                movements.Select(m => m.Id.ToString()).ToList(),
                production.Select(p => p.ProductionOrderId).Distinct().ToList()));

        return new DayOperationalPicture(
            employees,
            production,
            inventory,
            figures,
            issues,
            activeSessions.Count,
            activeSessions);
    }

    public async Task<MonthOperationalPicture> LoadMonthAsync(
        int year,
        int month,
        bool allowNegativeStock,
        CancellationToken cancellationToken = default)
    {
        var start = new DateTimeOffset(year, month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        var jobs = await _jobs.Query().AsNoTracking()
            .Where(j => j.Status != ManufacturingJobStatuses.Cancelled && j.Status != ManufacturingJobStatuses.Draft)
            .ToListAsync(cancellationToken);
        var openJobs = jobs.Where(j => j.Status is not ManufacturingJobStatuses.Completed).ToList();
        var jobIds = openJobs.Select(j => j.Id).ToList();
        var tasks = jobIds.Count == 0
            ? []
            : await _tasks.Query().AsNoTracking()
                .Where(t => jobIds.Contains(t.JobId)
                    && t.Status != ManufacturingTaskStatuses.Skipped
                    && t.Status != ManufacturingTaskStatuses.Cancelled)
                .ToListAsync(cancellationToken);
        var monthTaskIds = tasks.Select(t => t.Id).ToList();
        var monthUnits = monthTaskIds.Count == 0
            ? []
            : await _units.Query().AsNoTracking().Where(u => monthTaskIds.Contains(u.TaskId)).ToListAsync(cancellationToken);
        var progressByTask = monthUnits
            .GroupBy(u => u.TaskId)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(u => u.ProgressPercentage), 2));
        var jobsById = openJobs.ToDictionary(j => j.Id);
        var production = tasks.Select(task =>
        {
            var job = jobsById[task.JobId];
            var progress = progressByTask.GetValueOrDefault(task.Id);
            var completedQty = Math.Round(job.Quantity * progress / 100m, 4);
            var wipQty = Math.Max(0, job.Quantity - completedQty);
            var rate = task.LabourCostRate is > 0 ? task.LabourCostRate.Value : 500m;
            var actualCost = Math.Round(task.ActualHours * rate, 2);
            var estimated = Math.Round(task.EstimatedHours * rate, 2);
            var isOpen = task.Status is not ManufacturingTaskStatuses.Completed;
            return new ProductionMonthlySnapshotDto(
                Guid.NewGuid(),
                Guid.Empty,
                year,
                month,
                job.Id.ToString(),
                job.Number,
                task.Id.ToString(),
                task.Name,
                job.Quantity,
                completedQty,
                isOpen ? wipQty : 0,
                progress,
                0,
                task.ActualHours,
                estimated,
                actualCost,
                isOpen ? actualCost : 0,
                DateTimeOffset.UtcNow);
        }).ToList();

        var invoices = await _invoices.Query().AsNoTracking()
            .Where(i => i.IssueDate >= start && i.IssueDate < end && i.Status != InvoiceStatuses.Void)
            .ToListAsync(cancellationToken);
        var creditNotes = await _creditNotes.Query().AsNoTracking()
            .Where(c => c.CreatedOnUtc >= start && c.CreatedOnUtc < end && c.Status != CreditNoteStatuses.Void && c.Status != CreditNoteStatuses.Draft)
            .ToListAsync(cancellationToken);
        var movements = await _movements.Query().AsNoTracking()
            .Where(m => m.PerformedAtUtc >= start && m.PerformedAtUtc < end)
            .ToListAsync(cancellationToken);
        var items = await _items.Query().AsNoTracking().ToListAsync(cancellationToken);
        var itemsById = items.ToDictionary(i => i.Id);

        var issues = new List<OperationalIssue>();
        foreach (var invoice in invoices.Where(i => i.Status == InvoiceStatuses.Draft))
        {
            issues.Add(new OperationalIssue(
                "INVOICE_UNPOSTED",
                "finance",
                $"Invoice {invoice.InvoiceNumber} has not been posted.",
                true,
                "invoice",
                invoice.Id.ToString()));
        }

        if (!allowNegativeStock)
        {
            foreach (var item in items.Where(i => i.QuantityOnHand < 0))
            {
                issues.Add(new OperationalIssue(
                    "NEGATIVE_STOCK",
                    "inventory",
                    $"{item.Sku} has invalid negative stock ({item.QuantityOnHand}).",
                    true,
                    "inventory_item",
                    item.Id.ToString()));
            }
        }

        var inProgress = jobs.Count(j => j.Status is ManufacturingJobStatuses.InProgress
            or ManufacturingJobStatuses.OnHold
            or ManufacturingJobStatuses.QualityCheck
            or ManufacturingJobStatuses.Rework);
        if (inProgress > 0)
        {
            issues.Add(new OperationalIssue(
                "PRODUCTION_IN_PROGRESS",
                "production",
                $"{inProgress} production job(s) remain in progress (allowed across month end).",
                false));
        }

        var inventory = items
            .Where(i => i.QuantityOnHand != 0 || movements.Any(m => m.InventoryItemId == i.Id))
            .Select(item =>
            {
                var related = movements.Where(m => m.InventoryItemId == item.Id).ToList();
                var received = related.Where(m => m.Type == StockMovementTypes.Receipt).Sum(m => m.Quantity);
                var consumed = related.Where(m => m.Type == StockMovementTypes.Issue).Sum(m => m.Quantity);
                var adjusted = related.Where(m => m.Type == StockMovementTypes.Adjustment).Sum(m => m.Quantity);
                var cost = item.CostPrice;
                return new InventoryMonthlySnapshotDto(
                    Guid.NewGuid(),
                    Guid.Empty,
                    year,
                    month,
                    item.Id.ToString(),
                    item.Sku,
                    item.Name,
                    item.Unit,
                    item.QuantityOnHand - received + consumed - adjusted,
                    Math.Round((item.QuantityOnHand - received + consumed - adjusted) * cost, 2),
                    received,
                    Math.Round(received * cost, 2),
                    consumed,
                    Math.Round(consumed * cost, 2),
                    adjusted,
                    Math.Round(adjusted * cost, 2),
                    item.QuantityOnHand,
                    Math.Round(item.QuantityOnHand * cost, 2),
                    DateTimeOffset.UtcNow);
            })
            .ToList();

        var posted = invoices.Where(i => i.Status != InvoiceStatuses.Draft).ToList();
        var sales = posted.Sum(i => i.TotalAmount);
        var payments = posted.Sum(i => i.AmountPaid);
        var credits = creditNotes.Sum(c => c.TotalAmount);
        var cogs = movements
            .Where(m => m.Type == StockMovementTypes.Issue && itemsById.ContainsKey(m.InventoryItemId))
            .Sum(m => m.Quantity * itemsById[m.InventoryItemId].CostPrice);
        var inventoryValue = items.Sum(i => i.QuantityOnHand * i.CostPrice);
        var wip = production.Sum(p => p.WipCost);
        var labour = production.Sum(p => p.ActualCostToDate);
        var gross = sales - cogs;
        var figures = new MonthlyActivityFigures(
            sales,
            0,
            payments,
            0,
            Math.Round(inventoryValue, 2),
            wip,
            Math.Round(cogs, 2),
            Math.Round(gross, 2),
            Math.Round(cogs, 2),
            labour,
            labour,
            0,
            0,
            0,
            credits,
            Math.Round(gross - credits, 2),
            new MonthlyTransactionRefsDto(
                posted.Select(i => i.Id.ToString()).ToList(),
                posted.Where(i => i.AmountPaid > 0).Select(i => i.Id.ToString()).ToList(),
                [],
                [],
                inventory.Select(i => i.InventoryItemId).ToList(),
                production.Select(p => p.ProductionOrderId).Distinct().ToList()));

        return new MonthOperationalPicture(production, inventory, figures, issues, inProgress);
    }

    public async Task<IReadOnlyList<SessionCheckpointSeed>> PauseOpenWorkAsync(
        string businessDate,
        string reason,
        CancellationToken cancellationToken = default)
    {
        var day = DateOnly.Parse(businessDate);
        var now = DateTimeOffset.UtcNow;
        var seeds = new List<SessionCheckpointSeed>();

        var sessions = await _sessions.Query()
            .Where(s => s.BusinessDate == day && s.EndedAtUtc == null)
            .ToListAsync(cancellationToken);
        var tasks = await _tasks.Query()
            .Where(t => t.Status == ManufacturingTaskStatuses.InProgress)
            .ToListAsync(cancellationToken);
        var jobIds = tasks.Select(t => t.JobId).Distinct().ToList();
        var jobs = jobIds.Count == 0
            ? []
            : await _jobs.Query().Where(j => jobIds.Contains(j.Id)).ToListAsync(cancellationToken);
        var taskIds = tasks.Select(t => t.Id).ToList();
        var units = taskIds.Count == 0
            ? []
            : await _units.Query().Where(u => taskIds.Contains(u.TaskId)).ToListAsync(cancellationToken);
        var unitIds = units.Select(u => u.Id).ToList();
        var assignments = unitIds.Count == 0
            ? []
            : await _assignments.Query()
                .Where(a => unitIds.Contains(a.TaskUnitId) && a.Status == TaskUnitAssignmentStatuses.InProgress)
                .ToListAsync(cancellationToken);

        var jobsById = jobs.ToDictionary(j => j.Id);
        var tasksById = tasks.ToDictionary(t => t.Id);

        foreach (var session in sessions.Where(s => s.Status is EmployeeWorkSessionStatuses.Working or EmployeeWorkSessionStatuses.Started))
        {
            session.Pause(now, reason);
            var task = session.TaskId.HasValue && tasksById.TryGetValue(session.TaskId.Value, out var found) ? found : null;
            var job = jobsById.GetValueOrDefault(session.JobId);
            seeds.Add(new SessionCheckpointSeed(
                session.Id.ToString(),
                session.UserId.ToString(),
                session.UserName,
                job?.Id.ToString() ?? session.JobId.ToString(),
                task?.Id.ToString() ?? session.TaskId?.ToString() ?? string.Empty,
                task?.Name ?? "Work session",
                task?.OverallProgress ?? 0,
                session.StartedAtUtc,
                "paused"));
        }

        foreach (var task in tasks)
        {
            task.SetStatus(ManufacturingTaskStatuses.Paused, now);
            var job = jobsById.GetValueOrDefault(task.JobId);
            if (seeds.All(s => s.OperationId != task.Id.ToString()))
            {
                seeds.Add(new SessionCheckpointSeed(
                    task.Id.ToString(),
                    task.AssignedToUserId?.ToString() ?? string.Empty,
                    task.AssignedToName ?? string.Empty,
                    job?.Id.ToString() ?? task.JobId.ToString(),
                    task.Id.ToString(),
                    task.Name,
                    task.OverallProgress,
                    task.StartedAtUtc ?? now,
                    "paused"));
            }
        }

        foreach (var assignment in assignments)
            assignment.SetStatus(TaskUnitAssignmentStatuses.Paused, now);

        return seeds;
    }

    private static List<ProductionDailySnapshotDto> BuildDailyProduction(
        string businessDate,
        DateOnly day,
        Dictionary<Guid, ManufacturingJob> jobsById,
        List<ManufacturingTask> tasks,
        Dictionary<Guid, List<TaskUnit>> unitsByTask,
        Dictionary<Guid, List<TaskUnitAssignment>> assignmentsByUnit,
        List<EmployeeWorkSession> sessions)
    {
        var rows = new List<ProductionDailySnapshotDto>();
        foreach (var task in tasks)
        {
            if (task.Status is ManufacturingTaskStatuses.Skipped or ManufacturingTaskStatuses.Cancelled or ManufacturingTaskStatuses.Pending)
                continue;
            if (!jobsById.TryGetValue(task.JobId, out var job))
                continue;

            var taskUnits = unitsByTask.GetValueOrDefault(task.Id) ?? [];
            var taskAssignments = taskUnits.SelectMany(u => assignmentsByUnit.GetValueOrDefault(u.Id) ?? []).ToList();
            var touchesDay = taskAssignments.Any(a => TouchesDay(a.StartedAtUtc, a.CompletedAtUtc, a.Status, day))
                || (task.StartedAtUtc.HasValue && DateOnly.FromDateTime(task.StartedAtUtc.Value.UtcDateTime) == day)
                || (task.CompletedAtUtc.HasValue && DateOnly.FromDateTime(task.CompletedAtUtc.Value.UtcDateTime) == day)
                || task.Status is ManufacturingTaskStatuses.InProgress or ManufacturingTaskStatuses.Paused or ManufacturingTaskStatuses.OnHold;
            if (!touchesDay)
                continue;

            var completedUnits = taskUnits.Count(u => u.IsComplete);
            var partialUnits = taskUnits.Count(u => !u.IsComplete && !u.IsCancelled && u.ProgressPercentage > 0);
            var progress = taskUnits.Count == 0
                ? 0
                : Math.Round(taskUnits.Average(u => u.ProgressPercentage), 2);
            var worker = taskAssignments.FirstOrDefault(a => a.Status == TaskUnitAssignmentStatuses.InProgress)
                ?? taskAssignments.FirstOrDefault();
            var worked = sessions.Where(s => s.TaskId == task.Id).Sum(s => LiveWorkedMinutes(s, DateTimeOffset.UtcNow));
            if (worked == 0)
                worked = (int)Math.Round(taskAssignments.Sum(a => a.ActualHours) * 60);

            rows.Add(new ProductionDailySnapshotDto(
                Guid.NewGuid(),
                Guid.Empty,
                businessDate,
                job.Id.ToString(),
                job.Number,
                task.Id.ToString(),
                task.Name,
                worker?.UserId.ToString(),
                worker?.UserName ?? task.AssignedToName,
                job.Quantity,
                completedUnits,
                partialUnits,
                progress,
                worked,
                completedUnits,
                task.RejectedQuantity,
                job.Status,
                task.Status,
                DateTimeOffset.UtcNow));
        }

        return rows;
    }

    private static List<EmployeeDayWorkSummaryDto> BuildEmployees(
        string businessDate,
        DateOnly day,
        PeriodClosePolicy policy,
        Dictionary<Guid, ManufacturingJob> jobsById,
        Dictionary<Guid, ManufacturingTask> tasksById,
        List<TaskUnit> units,
        List<TaskUnitAssignment> assignments,
        List<EmployeeWorkSession> sessions)
    {
        var unitTask = units.ToDictionary(u => u.Id, u => u.TaskId);
        var people = new Dictionary<Guid, PersonWork>();

        foreach (var assignment in assignments)
        {
            if (!TouchesDay(assignment.StartedAtUtc, assignment.CompletedAtUtc, assignment.Status, day))
                continue;
            if (!unitTask.TryGetValue(assignment.TaskUnitId, out var taskId))
                continue;
            if (!tasksById.TryGetValue(taskId, out var task))
                continue;
            var bucket = people.GetValueOrDefault(assignment.UserId) ?? new PersonWork(assignment.UserId, assignment.UserName);
            var minutes = (int)Math.Round(assignment.ActualHours * 60);
            if (minutes == 0 && assignment.Status == TaskUnitAssignmentStatuses.InProgress && assignment.StartedAtUtc.HasValue)
                minutes = (int)Math.Max(0, (DateTimeOffset.UtcNow - assignment.StartedAtUtc.Value).TotalMinutes);
            bucket.WorkedMinutes += minutes;
            bucket.NormalOvertimeMinutes += (int)Math.Round(assignment.NormalOvertimeHours * 60);
            bucket.DoubleOvertimeMinutes += (int)Math.Round(assignment.DoubleOvertimeHours * 60);
            bucket.LaborCost += assignment.LaborCost;
            bucket.StartedAt ??= assignment.StartedAtUtc;
            var job = jobsById.GetValueOrDefault(task.JobId);
            bucket.Sessions.Add(new EmployeeWorkSessionDto(
                assignment.Id,
                assignment.UserId.ToString(),
                assignment.UserName,
                businessDate,
                task.Id.ToString(),
                task.Name,
                job?.Id.ToString(),
                job?.Number,
                assignment.StartedAtUtc ?? DateTimeOffset.UtcNow,
                assignment.CompletedAtUtc,
                MapAssignmentStatus(assignment.Status),
                minutes,
                0,
                0));
            people[assignment.UserId] = bucket;
        }

        foreach (var session in sessions)
        {
            var bucket = people.GetValueOrDefault(session.UserId) ?? new PersonWork(session.UserId, session.UserName);
            if (bucket.Sessions.Count == 0)
            {
                var minutes = LiveWorkedMinutes(session, DateTimeOffset.UtcNow);
                bucket.WorkedMinutes += minutes;
                var task = session.TaskId.HasValue ? tasksById.GetValueOrDefault(session.TaskId.Value) : null;
                var job = jobsById.GetValueOrDefault(session.JobId);
                bucket.Sessions.Add(new EmployeeWorkSessionDto(
                    session.Id,
                    session.UserId.ToString(),
                    session.UserName,
                    businessDate,
                    task?.Id.ToString(),
                    task?.Name,
                    job?.Id.ToString() ?? session.JobId.ToString(),
                    job?.Number,
                    session.StartedAtUtc,
                    session.EndedAtUtc,
                    session.Status,
                    minutes,
                    session.PauseMinutes,
                    0));
            }

            people[session.UserId] = bucket;
        }

        return people.Values
            .Select(person => ToSummary(person, businessDate, policy))
            .OrderBy(s => s.EmployeeName)
            .ToList();
    }

    private static EmployeeDayWorkSummaryDto ToSummary(PersonWork person, string businessDate, PeriodClosePolicy policy)
    {
        var required = Math.Max(0, policy.RequiredDailyWorkMinutes);
        var worked = Math.Max(0, person.WorkedMinutes);
        var regular = Math.Min(worked, required);
        var excess = Math.Max(0, worked - required);
        var normalOt = person.NormalOvertimeMinutes;
        var doubleOt = person.DoubleOvertimeMinutes;
        if (normalOt + doubleOt == 0 && excess > 0)
        {
            var doubleAfter = policy.DoubleOvertimeAfterMinutes;
            if (doubleAfter > required)
            {
                var normalCap = doubleAfter - required;
                normalOt = Math.Min(excess, normalCap);
                doubleOt = Math.Max(0, excess - normalCap);
            }
            else
            {
                normalOt = excess;
            }
        }

        var rate = policy.LabourRatePerHour > 0 ? policy.LabourRatePerHour : 500m;
        var regularCost = person.LaborCost > 0 ? person.LaborCost : Math.Round(regular / 60m * rate, 2);
        var normalCost = Math.Round(normalOt / 60m * rate * NormalOvertimeMultiplier, 2);
        var doubleCost = Math.Round(doubleOt / 60m * rate * DoubleOvertimeMultiplier, 2);
        var overtime = normalOt + doubleOt;
        var complete = worked >= required && required > 0 || (required == 0 && worked >= 0 && person.Sessions.Count > 0 && worked > 0);
        if (required == 0)
            complete = true;

        var breakdown = person.Sessions
            .GroupBy(s => s.TaskId ?? s.Id.ToString())
            .Select(g => new EmployeeTaskWorkBreakdownDto(
                g.Key,
                g.First().TaskName ?? "Work session",
                g.First().ProductionOrderId,
                g.First().ProductionOrderNumber,
                g.Sum(s => s.WorkedMinutes)))
            .ToList();

        return new EmployeeDayWorkSummaryDto(
            person.UserId.ToString(),
            person.UserName,
            businessDate,
            required,
            worked,
            regular,
            normalOt,
            doubleOt,
            overtime,
            0,
            0,
            worked,
            Math.Max(0, required - worked),
            complete ? "completed" : "incomplete",
            complete,
            regularCost,
            normalCost,
            doubleCost,
            normalCost + doubleCost,
            regularCost + normalCost + doubleCost,
            person.Sessions,
            breakdown);
    }

    private static List<SessionCheckpointSeed> BuildActiveSessions(
        DateOnly day,
        Dictionary<Guid, ManufacturingJob> jobsById,
        Dictionary<Guid, ManufacturingTask> tasksById,
        List<TaskUnit> units,
        List<TaskUnitAssignment> assignments,
        List<EmployeeWorkSession> sessions)
    {
        var seeds = new List<SessionCheckpointSeed>();
        var unitTask = units.ToDictionary(u => u.Id, u => u.TaskId);
        foreach (var assignment in assignments.Where(a => a.Status is TaskUnitAssignmentStatuses.InProgress or TaskUnitAssignmentStatuses.Paused or TaskUnitAssignmentStatuses.OnHold))
        {
            if (!TouchesDay(assignment.StartedAtUtc, assignment.CompletedAtUtc, assignment.Status, day))
                continue;
            if (!unitTask.TryGetValue(assignment.TaskUnitId, out var taskId) || !tasksById.TryGetValue(taskId, out var task))
                continue;
            var job = jobsById.GetValueOrDefault(task.JobId);
            seeds.Add(new SessionCheckpointSeed(
                assignment.Id.ToString(),
                assignment.UserId.ToString(),
                assignment.UserName,
                job?.Id.ToString() ?? task.JobId.ToString(),
                task.Id.ToString(),
                task.Name,
                task.OverallProgress,
                assignment.StartedAtUtc ?? DateTimeOffset.UtcNow,
                assignment.Status == TaskUnitAssignmentStatuses.InProgress ? "paused" : "paused"));
        }

        foreach (var session in sessions.Where(s => s.Status is EmployeeWorkSessionStatuses.Working or EmployeeWorkSessionStatuses.Started or EmployeeWorkSessionStatuses.Paused))
        {
            if (seeds.Any(s => s.WorkerId == session.UserId.ToString() && s.OperationId == session.TaskId?.ToString()))
                continue;
            var task = session.TaskId.HasValue ? tasksById.GetValueOrDefault(session.TaskId.Value) : null;
            var job = jobsById.GetValueOrDefault(session.JobId);
            seeds.Add(new SessionCheckpointSeed(
                session.Id.ToString(),
                session.UserId.ToString(),
                session.UserName,
                job?.Id.ToString() ?? session.JobId.ToString(),
                task?.Id.ToString() ?? string.Empty,
                task?.Name ?? "Work session",
                task?.OverallProgress ?? 0,
                session.StartedAtUtc,
                session.Status is EmployeeWorkSessionStatuses.Working or EmployeeWorkSessionStatuses.Started ? "awaiting_confirm" : "paused"));
        }

        return seeds;
    }

    private static List<InventoryDailySnapshotDto> BuildDailyInventory(
        string businessDate,
        List<StockMovement> movements,
        List<InventoryItem> items)
    {
        var itemsById = items.ToDictionary(i => i.Id);
        return movements
            .GroupBy(m => m.InventoryItemId)
            .Select(group =>
            {
                itemsById.TryGetValue(group.Key, out var item);
                var receipts = group.Where(m => m.Type == StockMovementTypes.Receipt).Sum(m => m.Quantity);
                var issues = group.Where(m => m.Type == StockMovementTypes.Issue).Sum(m => m.Quantity);
                var adjustments = group.Where(m => m.Type == StockMovementTypes.Adjustment).Sum(m => m.Quantity);
                var closing = item?.QuantityOnHand ?? 0;
                return new InventoryDailySnapshotDto(
                    Guid.NewGuid(),
                    Guid.Empty,
                    businessDate,
                    group.Key.ToString(),
                    item?.Sku ?? group.First().InventoryItemSku,
                    item?.Name ?? group.First().InventoryItemName,
                    item?.Unit ?? group.First().Unit,
                    closing - receipts + issues - adjustments,
                    receipts,
                    0,
                    0,
                    issues,
                    issues,
                    0,
                    adjustments,
                    closing,
                    group.Select(m => m.Id.ToString()).ToList(),
                    DateTimeOffset.UtcNow);
            })
            .ToList();
    }

    private static bool TouchesDay(DateTimeOffset? started, DateTimeOffset? completed, string status, DateOnly day)
    {
        if (started is null)
            return status is TaskUnitAssignmentStatuses.InProgress or TaskUnitAssignmentStatuses.Paused or TaskUnitAssignmentStatuses.OnHold;
        var startDate = DateOnly.FromDateTime(started.Value.UtcDateTime);
        if (startDate == day)
            return true;
        if (completed.HasValue && DateOnly.FromDateTime(completed.Value.UtcDateTime) == day)
            return true;
        return startDate < day
            && completed is null
            && status is TaskUnitAssignmentStatuses.InProgress or TaskUnitAssignmentStatuses.Paused or TaskUnitAssignmentStatuses.OnHold;
    }

    private static int LiveWorkedMinutes(EmployeeWorkSession session, DateTimeOffset now)
    {
        var minutes = session.WorkedMinutes;
        if (session.Status is EmployeeWorkSessionStatuses.Working or EmployeeWorkSessionStatuses.Started
            && session.LastWorkStartedAtUtc.HasValue)
        {
            minutes += (int)Math.Max(0, (now - session.LastWorkStartedAtUtc.Value).TotalMinutes);
        }

        return minutes;
    }

    private static string MapAssignmentStatus(string status) => status switch
    {
        TaskUnitAssignmentStatuses.InProgress => EmployeeWorkSessionStatuses.Working,
        TaskUnitAssignmentStatuses.Paused => EmployeeWorkSessionStatuses.Paused,
        TaskUnitAssignmentStatuses.OnHold => EmployeeWorkSessionStatuses.OnHold,
        TaskUnitAssignmentStatuses.Completed => EmployeeWorkSessionStatuses.Completed,
        _ => EmployeeWorkSessionStatuses.Started,
    };

    private static (DateTimeOffset Start, DateTimeOffset End) DayRange(string businessDate)
    {
        var date = DateOnly.Parse(businessDate);
        var start = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
        return (start, start.AddDays(1));
    }

    private sealed class PersonWork(Guid userId, string userName)
    {
        public Guid UserId { get; } = userId;
        public string UserName { get; } = userName;
        public int WorkedMinutes { get; set; }
        public int NormalOvertimeMinutes { get; set; }
        public int DoubleOvertimeMinutes { get; set; }
        public decimal LaborCost { get; set; }
        public DateTimeOffset? StartedAt { get; set; }
        public List<EmployeeWorkSessionDto> Sessions { get; } = [];
    }
}
