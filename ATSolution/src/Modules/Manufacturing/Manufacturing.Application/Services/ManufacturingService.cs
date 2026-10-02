using ATSolution.Application;
using ATSolution.Application.Abstractions.Periods;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Models;
using Catalog.Domain.Common;
using Catalog.Domain.Products;
using Identity.Domain.Users;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Manufacturing.Application.Abstractions;
using Manufacturing.Application.Common;
using Manufacturing.Application.Jobs;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Sequences;
using Manufacturing.Domain.Tasks;
using Manufacturing.Domain.Work;
using Microsoft.EntityFrameworkCore;
using Sales.Domain.Common;
using Sales.Domain.SalesOrders;

namespace Manufacturing.Application.Services;

public sealed class ManufacturingService : IManufacturingService
{
    private const string ReferenceType = "manufacturing_job";

    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<User, Guid> _users;
    private readonly IRepository<InventoryItem, Guid> _inventoryItems;
    private readonly IRepository<StockMovement, Guid> _stockMovements;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IRepository<EmployeeWorkSession, Guid> _sessions;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;
    private readonly IBusinessPeriodGuard _periodGuard;

    public ManufacturingService(
        IRepository<ManufacturingJob, Guid> jobs,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<Product, Guid> products,
        IRepository<User, Guid> users,
        IRepository<InventoryItem, Guid> inventoryItems,
        IRepository<StockMovement, Guid> stockMovements,
        IRepository<DocumentSequence, Guid> sequences,
        IRepository<EmployeeWorkSession, Guid> sessions,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator,
        IBusinessPeriodGuard periodGuard)
    {
        _jobs = jobs;
        _salesOrders = salesOrders;
        _products = products;
        _users = users;
        _inventoryItems = inventoryItems;
        _stockMovements = stockMovements;
        _sequences = sequences;
        _sessions = sessions;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _periodGuard = periodGuard;
    }

    public async Task<PaginatedResponse<ManufacturingJobDto>> ListAsync(
        ManufacturingListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _jobs.Query()
            .AsNoTracking()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Units)
            .ThenInclude(u => u.Assignments)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.History)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (query.SalesOrderId.HasValue)
            items = items.Where(x => x.SalesOrderId == query.SalesOrderId);
        if (query.AssignedTo.HasValue)
            items = items.Where(x => x.AssignedToUserId == query.AssignedTo);
        if (!string.IsNullOrWhiteSpace(query.Priority))
            items = items.Where(x => x.Priority == query.Priority);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.Number.Contains(search)
                || x.ProductName.Contains(search)
                || x.ProductSku.Contains(search)
                || x.CustomerName.Contains(search)
                || x.SalesOrderNumber.Contains(search));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var total = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<ManufacturingJobDto>.Create(
            pageItems.Select(ManufacturingMappers.MapJob).ToList(),
            total,
            page,
            pageSize);
    }

    public async Task<ManufacturingJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var job = await LoadJobAsync(id, cancellationToken, asNoTracking: true);
        return job is null ? null : ManufacturingMappers.MapJob(job);
    }

    public async Task<ManufacturingJobDto> CreateAsync(
        CreateManufacturingJobCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await _salesOrders.Query()
                .Include(o => o.Lines)
                .FirstOrDefaultAsync(o => o.Id == command.SalesOrderId, ct)
                ?? throw new NotFoundException($"Sales order '{command.SalesOrderId}' was not found.");

            if (!SalesOrderStatuses.CanManufacture(order.Status))
            {
                ManufacturingErrors.InvalidState(
                    "Confirm the sales order (after costing approval) before creating a manufacturing job.");
            }

            var product = await _products.Query()
                .Include(p => p.Versions)
                .FirstOrDefaultAsync(p => p.Id == command.ProductId, ct)
                ?? throw new NotFoundException($"Product '{command.ProductId}' was not found.");

            var version = command.ProductVersionId.HasValue
                ? product.Versions.FirstOrDefault(v => v.Id == command.ProductVersionId.Value)
                : product.Versions.FirstOrDefault(v => v.Status == ProductVersionStatuses.Released)
                  ?? product.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();

            if (version is null)
            {
                throw new ApplicationValidationException(
                    [new ValidationError("productVersionId", "No product version available for manufacturing.", "INVALID_STATE")]);
            }

            var line = order.Lines.FirstOrDefault(l =>
                l.ProductId == command.ProductId &&
                (command.ProductVersionId == null || l.ProductVersionId == command.ProductVersionId));

            string? assigneeName = null;
            if (command.AssignedTo.HasValue)
            {
                var user = await _users.Query()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Id == command.AssignedTo.Value, ct);
                assigneeName = user?.FullName;
            }

            var bom = JsonColumn.Deserialize(version.BomJson, Array.Empty<BomItemJson>());
            var operations = JsonColumn.Deserialize(version.OperationsJson, Array.Empty<ProductOperationJson>());
            var materialRequirements = bom.Select(item => new MaterialRequirementDto(
                Guid.NewGuid(),
                item.InventoryItemId,
                item.Sku,
                item.InventoryItemName,
                Math.Round((item.RequiredQuantity > 0 ? item.RequiredQuantity : item.Quantity) * command.Quantity, 4),
                0,
                0,
                item.Unit,
                MaterialRequirementStatuses.Pending,
                null,
                null,
                null)).ToList();

            var number = await DocumentNumberGenerator.NextManufacturingJobNumberAsync(_sequences, ct);
            var job = ManufacturingJob.Create(
                number,
                order.Id,
                order.Number,
                order.CustomerId,
                order.CustomerName,
                product.Id,
                product.Sku,
                product.Name,
                version.Id,
                version.Label,
                command.Quantity,
                command.Priority,
                command.PlannedStartDate,
                command.PlannedEndDate,
                command.AssignedTo,
                assigneeName,
                command.Notes,
                JsonColumn.Serialize(materialRequirements),
                command.CreatedBy ?? "system",
                command.CreatedByName ?? "System");

            var actor = new TaskActionActorDto(
                Guid.TryParse(command.CreatedBy, out var uid) ? uid : Guid.Empty,
                command.CreatedByName ?? "System");

            CreateTasksFromOperations(job, (int)Math.Floor(command.Quantity), operations, actor);
            await _jobs.AddAsync(job, ct);

            if (line is not null)
            {
                line.AllocateToManufacturing(command.Quantity);
            }

            order.LinkManufacturingJob(job.Id);
            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> UpdateAsync(
        UpdateManufacturingJobCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(command.Id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{command.Id}' was not found.");

            job.Update(
                command.Priority,
                command.PlannedStartDate,
                command.PlannedEndDate,
                command.AssignedTo,
                command.AssignedToName,
                command.Notes,
                command.Status);

            if (command.QualityInspection is not null)
            {
                job.SetQualityInspectionJson(JsonColumn.Serialize(command.QualityInspection));
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{id}' was not found.");

            if (job.Status != ManufacturingJobStatuses.Draft)
            {
                throw new ApplicationValidationException(
                    [new ValidationError("status", "Only draft jobs can be deleted.", "INVALID_STATE")]);
            }

            _jobs.Remove(job);
            await _unitOfWork.SaveChangesAsync(ct);
            return true;
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> ReserveMaterialsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{id}' was not found.");

            var requirements = JsonColumn.Deserialize(job.MaterialRequirementsJson, new List<MaterialRequirementDto>());
            var performedBy = job.CreatedBy;
            var performedByName = job.CreatedByName;
            var updatedRequirements = new List<MaterialRequirementDto>();

            foreach (var mr in requirements)
            {
                if (mr.Status == MaterialRequirementStatuses.Issued ||
                    mr.ReservedQuantity >= mr.RequiredQuantity)
                {
                    updatedRequirements.Add(mr with
                    {
                        ReservedQuantity = Math.Max(mr.ReservedQuantity, mr.RequiredQuantity),
                        Status = mr.Status == MaterialRequirementStatuses.Issued
                            ? MaterialRequirementStatuses.Issued
                            : MaterialRequirementStatuses.Reserved,
                    });
                    continue;
                }

                var qty = Math.Max(0, mr.RequiredQuantity - mr.ReservedQuantity);
                if (qty > 0)
                {
                    var item = await _inventoryItems.Query()
                        .FirstOrDefaultAsync(i => i.Id == mr.InventoryItemId, ct)
                        ?? throw new ApplicationValidationException(
                            [new ValidationError("inventory", $"Inventory item '{mr.InventoryItemSku}' was not found.", "NOT_FOUND")]);

                    item.ApplyReservation(qty);
                    await _stockMovements.AddAsync(StockMovement.Create(
                        item.Id,
                        item.Name,
                        item.Sku,
                        StockMovementTypes.Reservation,
                        qty,
                        mr.Unit,
                        ReferenceType,
                        job.Id.ToString(),
                        $"Reserve for {job.Number}",
                        performedBy,
                        performedByName,
                        null));
                }

                updatedRequirements.Add(mr with
                {
                    ReservedQuantity = mr.RequiredQuantity,
                    Status = MaterialRequirementStatuses.Reserved,
                });
            }

            job.SetMaterialRequirementsJson(JsonColumn.Serialize(updatedRequirements));
            if (job.Status is ManufacturingJobStatuses.Draft or ManufacturingJobStatuses.Planned or ManufacturingJobStatuses.MaterialsPending)
            {
                job.SetStatus(ManufacturingJobStatuses.ReadyToStart);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> StartJobAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{id}' was not found.");

            if (job.Status is ManufacturingJobStatuses.Completed or ManufacturingJobStatuses.Cancelled)
            {
                ManufacturingErrors.InvalidState("A completed or cancelled job cannot be started.");
            }

            var order = await _salesOrders.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == job.SalesOrderId, ct)
                ?? throw new NotFoundException($"Sales order '{job.SalesOrderId}' was not found.");

            if (!SalesOrderStatuses.CanManufacture(order.Status))
            {
                ManufacturingErrors.InvalidState(
                    "Confirm the sales order (after costing approval) before starting manufacturing.");
            }

            await IssueMaterialsAsync(job, ct);
            var now = DateTimeOffset.UtcNow;
            job.MarkStarted(now);
            ManufacturingJobHelper.ApplyTaskReadiness(job, new TaskActionActorDto(Guid.Empty, job.CreatedByName));
            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> CompleteJobAsync(
        Guid id,
        ProductionCompletionInputDto? completion,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{id}' was not found.");

            var inspection = JsonColumn.Deserialize<QualityInspectionDto?>(job.QualityInspectionJson, null);
            if (!ManufacturingJobHelper.IsProductionJobCompletable(job, inspection))
            {
                ManufacturingErrors.InvalidState(
                    "All required manufacturing tasks must be completed and QC must pass before the product can be completed.");
            }

            if (job.Status == ManufacturingJobStatuses.Completed &&
                !string.IsNullOrWhiteSpace(job.CompletionOutcomeJson))
            {
                return ManufacturingMappers.MapJob(job);
            }

            var requirements = JsonColumn.Deserialize(job.MaterialRequirementsJson, new List<MaterialRequirementDto>());
            if (requirements.Any(mr => mr.IssuedQuantity < mr.RequiredQuantity))
            {
                await IssueMaterialsAsync(job, ct);
                requirements = JsonColumn.Deserialize(job.MaterialRequirementsJson, new List<MaterialRequirementDto>());
            }

            ProductionCompletionInputDto effectiveCompletion = completion ?? new ProductionCompletionInputDto(
                requirements.Sum(mr => mr.IssuedQuantity),
                0,
                0,
                0,
                null);

            var outcome = await PostProductionOutcomeAsync(job, effectiveCompletion, ct);
            job.SetCompletionOutcomeJson(JsonColumn.Serialize(outcome));
            job.MarkCompleted(DateTimeOffset.UtcNow);
            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> HoldJobAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(id, ct)
                ?? throw new NotFoundException($"Manufacturing job '{id}' was not found.");

            if (job.Status is ManufacturingJobStatuses.Completed or ManufacturingJobStatuses.Cancelled)
            {
                ManufacturingErrors.InvalidState("A completed or cancelled job cannot be put on hold.");
            }

            job.SetStatus(ManufacturingJobStatuses.OnHold);
            if (!string.IsNullOrWhiteSpace(reason))
            {
                job.Update(null, null, null, null, null, string.Join("\n", new[] { job.Notes, reason }.Where(s => !string.IsNullOrWhiteSpace(s))), null);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> ApplyTaskActionAsync(
        Guid jobId,
        ManufacturingTaskActionDto action,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default)
    {
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(jobId, ct)
                ?? throw new NotFoundException($"Manufacturing job '{jobId}' was not found.");

            var inspection = JsonColumn.Deserialize<QualityInspectionDto?>(job.QualityInspectionJson, null);
            TaskActionProcessor.Apply(job, action, actor, inspection);
            await WorkSessionSync.ApplyAsync(_sessions, job, action, actor, ct);
            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    public async Task<ManufacturingJobDto> CompleteTasksAsync(
        Guid jobId,
        BulkCompleteTasksInputDto input,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default)
    {
        await _periodGuard.EnsureWritableAsync(DateTimeOffset.UtcNow, cancellationToken);
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var job = await LoadJobAsync(jobId, ct)
                ?? throw new NotFoundException($"Manufacturing job '{jobId}' was not found.");

            var entries = input.Tasks?.ToList() ?? [];
            if (entries.Count == 0 && input.TaskIds is { Count: > 0 })
            {
                entries = input.TaskIds.Select(id => new BulkCompleteTaskEntryDto(id, null, null, null, null, null, null, null, input.Notes)).ToList();
            }

            foreach (var entry in entries)
            {
                TaskActionProcessor.Apply(
                    job,
                    new ManufacturingTaskActionDto
                    {
                        Type = "complete",
                        TaskId = entry.TaskId,
                        CompletedQuantity = entry.CompletedQuantity,
                        RejectedQuantity = entry.RejectedQuantity,
                        WasteQuantity = entry.WasteQuantity,
                        Contributors = entry.Contributors,
                        ActualHours = entry.ActualHours,
                        NormalOvertimeHours = entry.NormalOvertimeHours,
                        DoubleOvertimeHours = entry.DoubleOvertimeHours,
                        Notes = entry.Notes ?? input.Notes,
                    },
                    actor,
                    JsonColumn.Deserialize<QualityInspectionDto?>(job.QualityInspectionJson, null));
                await WorkSessionSync.ApplyAsync(
                    _sessions,
                    job,
                    new ManufacturingTaskActionDto
                    {
                        Type = "complete",
                        TaskId = entry.TaskId,
                        Contributors = entry.Contributors,
                        Notes = entry.Notes ?? input.Notes,
                    },
                    actor,
                    ct);
            }

            await _unitOfWork.SaveChangesAsync(ct);
            return ManufacturingMappers.MapJob(job);
        }, cancellationToken);
    }

    private static void CreateTasksFromOperations(
        ManufacturingJob job,
        int quantity,
        IReadOnlyList<ProductOperationJson> operations,
        TaskActionActorDto actor)
    {
        var enabled = operations.Where(o => o.IsEnabled).OrderBy(o => o.Sequence).ToList();
        var opToTask = new Dictionary<Guid, Guid>();

        for (var index = 0; index < enabled.Count; index++)
        {
            var op = enabled[index];
            var task = ManufacturingTask.Create(
                job.Id,
                $"TASK-{index + 1:000}",
                op.Name,
                op.Sequence,
                Math.Round(op.EstimatedHours * quantity, 2),
                quantity,
                op.IsRequired,
                op.IsEnabled,
                ManufacturingJobHelper.IsQcOperation(op.Name, op.IsQualityCheck),
                op.Id,
                op.Description,
                op.LabourCostRate,
                op.MachineName,
                op.MachineCost.HasValue ? Math.Round(op.MachineCost.Value * quantity, 2) : null,
                JsonColumn.Serialize(op),
                null);
            opToTask[op.Id] = task.Id;
            task.AddHistory(TaskHistoryEntry.Create(
                task.Id,
                actor.UserId,
                actor.UserName,
                "created",
                null,
                ManufacturingTaskStatuses.Pending,
                "Generated from product version operation"));
            job.Tasks.Add(task);
        }

        for (var index = 0; index < enabled.Count; index++)
        {
            var op = enabled[index];
            var task = job.Tasks.ElementAt(index);
            IReadOnlyList<Guid> prereqOpIds = op.PrerequisiteOperationIds?.ToList() ?? [];
            if (prereqOpIds.Count == 0 && index > 0)
            {
                prereqOpIds = [enabled[index - 1].Id];
            }

            var prereqTaskIds = prereqOpIds
                .Where(opToTask.ContainsKey)
                .Select(id => opToTask[id])
                .ToList();
            task.SetPrerequisiteTaskIds(prereqTaskIds);
        }
    }

    private async Task IssueMaterialsAsync(ManufacturingJob job, CancellationToken ct)
    {
        var requirements = JsonColumn.Deserialize(job.MaterialRequirementsJson, new List<MaterialRequirementDto>());
        var updated = new List<MaterialRequirementDto>();

        foreach (var mr in requirements)
        {
            if (mr.Status == MaterialRequirementStatuses.Issued && mr.IssuedQuantity >= mr.RequiredQuantity)
            {
                updated.Add(mr);
                continue;
            }

            var qtyToIssue = Math.Max(0, mr.RequiredQuantity - mr.IssuedQuantity);
            if (qtyToIssue <= 0)
            {
                updated.Add(mr with { Status = MaterialRequirementStatuses.Issued });
                continue;
            }

            var item = await _inventoryItems.Query()
                .FirstOrDefaultAsync(i => i.Id == mr.InventoryItemId, ct)
                ?? throw new ApplicationValidationException(
                    [new ValidationError("inventory", $"No issuable inventory for {mr.InventoryItemSku}.", "INVALID_STATE")]);

            if (mr.ReservedQuantity > 0)
            {
                item.ApplyRelease(mr.ReservedQuantity);
                await _stockMovements.AddAsync(StockMovement.Create(
                    item.Id,
                    item.Name,
                    item.Sku,
                    StockMovementTypes.Release,
                    mr.ReservedQuantity,
                    mr.Unit,
                    ReferenceType,
                    job.Id.ToString(),
                    $"Release reservation for {job.Number}",
                    job.CreatedBy,
                    job.CreatedByName,
                    null));
            }

            item.ApplyIssue(qtyToIssue);
            var issueMovement = StockMovement.Create(
                item.Id,
                item.Name,
                item.Sku,
                StockMovementTypes.Issue,
                qtyToIssue,
                mr.Unit,
                ReferenceType,
                job.Id.ToString(),
                $"Production issue for {job.Number}",
                job.CreatedBy,
                job.CreatedByName,
                null);
            await _stockMovements.AddAsync(issueMovement);

            updated.Add(mr with
            {
                IssuedQuantity = mr.IssuedQuantity + qtyToIssue,
                ReservedQuantity = 0,
                Status = MaterialRequirementStatuses.Issued,
                IssuedFromInventoryItemId = item.Id,
                IssuedStockMovementId = issueMovement.Id,
                IssuedUnitCost = item.CostPrice,
            });
        }

        job.SetMaterialRequirementsJson(JsonColumn.Serialize(updated));
    }

    private async Task<ProductionMaterialOutcomeDto> PostProductionOutcomeAsync(
        ManufacturingJob job,
        ProductionCompletionInputDto completion,
        CancellationToken ct)
    {
        var scrapLotIds = new List<string>();
        var recoverableLotIds = new List<string>();
        var now = DateTimeOffset.UtcNow;

        if (completion.FinishedMaterialQuantity > 0)
        {
            var fg = await _inventoryItems.Query()
                .FirstOrDefaultAsync(i => i.Sku == job.ProductSku, ct);
            if (fg is not null)
            {
                fg.ApplyReceipt(completion.FinishedMaterialQuantity);
                await _stockMovements.AddAsync(StockMovement.Create(
                    fg.Id,
                    fg.Name,
                    fg.Sku,
                    StockMovementTypes.Receipt,
                    completion.FinishedMaterialQuantity,
                    fg.Unit,
                    ReferenceType,
                    job.Id.ToString(),
                    $"Finished goods receipt for {job.Number}",
                    job.CreatedBy,
                    job.CreatedByName,
                    null));
            }
        }

        if (completion.ReusableScrapQuantity > 0)
        {
            scrapLotIds.Add($"scrap-{job.Number}");
        }

        if (completion.RecoverableQuantity is > 0)
        {
            recoverableLotIds.Add($"recover-{job.Number}");
        }

        return new ProductionMaterialOutcomeDto(
            completion.FinishedMaterialQuantity,
            completion.ReusableScrapQuantity,
            completion.RecoverableQuantity ?? 0,
            completion.PermanentWasteQuantity,
            now,
            scrapLotIds,
            recoverableLotIds);
    }

    private async Task<ManufacturingJob?> LoadJobAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking = false)
    {
        IQueryable<ManufacturingJob> query = _jobs.Query()
            .Include(j => j.Tasks)
            .ThenInclude(t => t.Units)
            .ThenInclude(u => u.Assignments)
            .Include(j => j.Tasks)
            .ThenInclude(t => t.History);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(j => j.Id == id, cancellationToken);
    }
}
