using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Items;
using Inventory.Application.Reprocessing;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Reprocessing;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Application.Services;

public sealed class ReprocessingService : IReprocessingService
{
    private readonly IRepository<ReprocessingBatch, Guid> _batches;
    private readonly IRepository<InventoryItem, Guid> _items;
    private readonly IInventoryService _inventoryService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public ReprocessingService(
        IRepository<ReprocessingBatch, Guid> batches,
        IRepository<InventoryItem, Guid> items,
        IInventoryService inventoryService,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _batches = batches;
        _items = items;
        _inventoryService = inventoryService;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<ReprocessingBatchDto>> ListAsync(
        ReprocessingListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var batches = _batches.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            batches = batches.Where(b => b.Status == query.Status);

        if (query.InputScrapLotId.HasValue)
            batches = batches.Where(b => b.InputScrapLotId == query.InputScrapLotId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            batches = batches.Where(b =>
                b.BatchNumber.Contains(search)
                || b.InputScrapSku.Contains(search)
                || b.InputScrapName.Contains(search)
                || (b.RecoveredLotSku != null && b.RecoveredLotSku.Contains(search)));
        }

        batches = batches.OrderByDescending(b => b.CreatedOnUtc);
        var totalCount = await batches.CountAsync(cancellationToken);
        var pageItems = await batches.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<ReprocessingBatchDto>.Create(pageItems.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<ReprocessingBatchDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var batch = await _batches.Query().AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        return batch is null ? null : Map(batch);
    }

    public async Task<ReprocessingBatchDto> CreateAsync(
        CreateReprocessingBatchCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var scrap = await _items.Query()
            .FirstOrDefaultAsync(i => i.Id == command.InputScrapLotId, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{command.InputScrapLotId}' was not found.");

        if (!IsReusableScrap(scrap))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.InputScrapLotId),
                    "Reprocessing input must be a reusable scrap inventory lot.",
                    ValidationErrorCodes.InvalidState),
            ]);
        }

        if (command.InputQuantity > scrap.QuantityAvailable + 0.000000001m)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.InputQuantity),
                    $"Insufficient scrap. Available: {scrap.QuantityAvailable} {scrap.Unit}, requested: {command.InputQuantity}.",
                    "INSUFFICIENT_STOCK"),
            ]);
        }

        var costs = NormalizeCosts(command.Costs);
        var batchNumber = await NextBatchNumberAsync(cancellationToken);

        try
        {
            var batch = ReprocessingBatch.Create(
                batchNumber,
                scrap.Id,
                scrap.Sku,
                scrap.Name,
                command.InputQuantity,
                scrap.Unit,
                scrap.CostPrice,
                costs.Labour,
                costs.Electricity,
                costs.Machine,
                costs.Gas,
                costs.Furnace,
                costs.Subcontract,
                costs.Other,
                command.SourceProductionOrderId,
                command.Notes,
                command.CreatedBy ?? "system",
                command.CreatedByName ?? "System");

            await _batches.AddAsync(batch, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(batch);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("inputQuantity", ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }
    }

    public async Task<ReprocessingBatchDto> StartAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var batch = await _batches.Query()
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken)
            ?? throw new NotFoundException($"Reprocessing batch '{id}' was not found.");

        if (batch.Status == ReprocessingBatchStatuses.InProgress)
        {
            return Map(batch);
        }

        var scrap = await _items.Query()
            .FirstOrDefaultAsync(i => i.Id == batch.InputScrapLotId, cancellationToken)
            ?? throw new NotFoundException($"Inventory item '{batch.InputScrapLotId}' was not found.");

        if (scrap.QuantityAvailable < batch.InputQuantity)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    "inputQuantity",
                    $"Insufficient scrap to start. Available: {scrap.QuantityAvailable}, required: {batch.InputQuantity}.",
                    "INSUFFICIENT_STOCK"),
            ]);
        }

        Guid? issueMovementId = null;
        try
        {
            var movement = await _inventoryService.RecordMovementAsync(
                new RecordStockMovementCommand(
                    scrap.Id,
                    StockMovementTypes.Issue,
                    batch.InputQuantity,
                    "reprocessing_batch",
                    batch.Id.ToString(),
                    $"Reprocessing issue {batch.BatchNumber}",
                    null,
                    batch.CreatedBy,
                    batch.CreatedByName),
                cancellationToken);
            issueMovementId = movement.Id;

            batch.Start(issueMovementId);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(batch);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }
    }

    public async Task<ReprocessingBatchDto> CompleteAsync(
        CompleteReprocessingCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var batch = await _batches.Query()
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Reprocessing batch '{command.Id}' was not found.");

        if (batch.Status == ReprocessingBatchStatuses.Completed)
        {
            return Map(batch);
        }

        if (batch.Status == ReprocessingBatchStatuses.Draft)
        {
            await StartAsync(batch.Id, cancellationToken);
            batch = await _batches.Query()
                .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
                ?? throw new NotFoundException($"Reprocessing batch '{command.Id}' was not found.");
        }

        try
        {
            var costs = command.Costs;
            batch.Complete(
                command.RecoveredQuantity,
                command.ProcessLossQuantity,
                costs?.Labour,
                costs?.Electricity,
                costs?.Machine,
                costs?.Gas,
                costs?.Furnace,
                costs?.Subcontract,
                costs?.Other,
                recoveredLotId: null,
                recoveredLotSku: null,
                command.Notes);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(batch);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("recoveredQuantity", ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }
    }

    public async Task<ReprocessingBatchDto> CancelAsync(
        CancelReprocessingCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var batch = await _batches.Query()
            .FirstOrDefaultAsync(b => b.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Reprocessing batch '{command.Id}' was not found.");

        try
        {
            batch.Cancel(command.Reason);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Map(batch);
        }
        catch (InvalidOperationException ex)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError("status", ex.Message, ValidationErrorCodes.InvalidState),
            ]);
        }
    }

    public async Task<IReadOnlyList<ReusableScrapLotDto>> ListReusableScrapLotsAsync(
        CancellationToken cancellationToken = default)
    {
        var items = await _items.Query()
            .AsNoTracking()
            .Where(i => i.Status == EntityStatuses.Active)
            .Where(i =>
                i.ItemType == InventoryItemTypes.ReusableScrap
                || i.Sku.Contains("scrap")
                || i.Name.Contains("scrap"))
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        return items
            .Where(i => i.QuantityAvailable > 0)
            .Select(i => new ReusableScrapLotDto(
                i.Id,
                i.Sku,
                i.Name,
                i.QuantityAvailable,
                i.Unit,
                i.CostPrice))
            .ToList();
    }

    private async Task<string> NextBatchNumberAsync(CancellationToken cancellationToken)
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"RP-{year}-";
        var existing = await _batches.Query()
            .AsNoTracking()
            .Where(b => b.BatchNumber.StartsWith(prefix))
            .Select(b => b.BatchNumber)
            .ToListAsync(cancellationToken);

        var maxSeq = 0;
        foreach (var number in existing)
        {
            var suffix = number.Length > prefix.Length ? number[prefix.Length..] : string.Empty;
            if (int.TryParse(suffix, out var seq) && seq > maxSeq)
            {
                maxSeq = seq;
            }
        }

        return $"{prefix}{(maxSeq + 1):D4}";
    }

    private static ReprocessingCostBreakdownDto NormalizeCosts(ReprocessingCostBreakdownDto? costs) =>
        new(
            Math.Max(0, costs?.Labour ?? 0),
            Math.Max(0, costs?.Electricity ?? 0),
            Math.Max(0, costs?.Machine ?? 0),
            Math.Max(0, costs?.Gas ?? 0),
            Math.Max(0, costs?.Furnace ?? 0),
            Math.Max(0, costs?.Subcontract ?? 0),
            Math.Max(0, costs?.Other ?? 0));

    private static bool IsReusableScrap(InventoryItem item) =>
        string.Equals(item.ItemType, InventoryItemTypes.ReusableScrap, StringComparison.OrdinalIgnoreCase)
        || item.Sku.Contains("scrap", StringComparison.OrdinalIgnoreCase)
        || item.Name.Contains("scrap", StringComparison.OrdinalIgnoreCase);

    private static ReprocessingBatchDto Map(ReprocessingBatch batch) =>
        new(
            batch.Id,
            batch.BatchNumber,
            batch.Status,
            batch.InputScrapLotId,
            batch.InputScrapSku,
            batch.InputScrapName,
            batch.InputQuantity,
            batch.InputUnit,
            batch.InputUnitCost,
            batch.WipLotId,
            batch.IssueMovementId,
            new ReprocessingCostBreakdownDto(
                batch.CostLabour,
                batch.CostElectricity,
                batch.CostMachine,
                batch.CostGas,
                batch.CostFurnace,
                batch.CostSubcontract,
                batch.CostOther),
            batch.TotalProcessingCost,
            batch.RecoveredQuantity,
            batch.ProcessLossQuantity,
            batch.RecoveredUnitCost,
            batch.RecoveredLotId,
            batch.RecoveredLotSku,
            batch.SourceProductionOrderId,
            batch.Notes,
            batch.CreatedBy,
            batch.CreatedByName,
            batch.CreatedOnUtc,
            batch.StartedAtUtc,
            batch.CompletedAtUtc,
            batch.ModifiedOnUtc);
}
