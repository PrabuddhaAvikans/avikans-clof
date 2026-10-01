using System.Text.Json;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;
using Catalog.Domain.Products;
using Sales.Application.Abstractions;
using Sales.Application.Common;
using Sales.Application.Costing;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.SalesOrders;
using Sales.Domain.Sequences;

namespace Sales.Application.Services;

public sealed class CostingService : ICostingService
{
    private readonly IRepository<CostingRequest, Guid> _costingRequests;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<DocumentSequence, Guid> _sequences;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public CostingService(
        IRepository<CostingRequest, Guid> costingRequests,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<Product, Guid> products,
        IRepository<DocumentSequence, Guid> sequences,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _costingRequests = costingRequests;
        _salesOrders = salesOrders;
        _products = products;
        _sequences = sequences;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<CostingRequestDto>> ListAsync(
        CostingListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var items = _costingRequests.Query().AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
            items = items.Where(x => x.Status == query.Status);
        if (!string.IsNullOrWhiteSpace(query.CoatingStatus))
            items = items.Where(x => x.CoatingStatus == query.CoatingStatus);
        if (query.SalesOrderId.HasValue)
            items = items.Where(x => x.SalesOrderId == query.SalesOrderId);
        if (query.LinkedToSalesOrder == true)
            items = items.Where(x => true);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            items = items.Where(x =>
                x.Number.Contains(search)
                || x.CustomerName.Contains(search)
                || x.ProjectName.Contains(search)
                || x.SalesOrderNumber.Contains(search)
                || (x.QuotationNumber != null && x.QuotationNumber.Contains(search)));
        }

        items = items.OrderByDescending(x => x.ModifiedOnUtc);
        var totalCount = await items.CountAsync(cancellationToken);
        var pageItems = await items.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var mapped = new List<CostingRequestDto>(pageItems.Count);
        foreach (var item in pageItems)
        {
            mapped.Add(CostingBuilder.Map(await RepairPlaceholderAsync(item, cancellationToken)));
        }

        return PaginatedResponse<CostingRequestDto>.Create(mapped, totalCount, page, pageSize);
    }

    public async Task<CostingRequestDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await _costingRequests.Query().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : CostingBuilder.Map(await RepairPlaceholderAsync(entity, cancellationToken));
    }

    public async Task<CostingRequestDto?> GetBySalesOrderIdAsync(
        Guid salesOrderId,
        CancellationToken cancellationToken = default)
    {
        var entity = await _costingRequests.Query().AsNoTracking()
            .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId, cancellationToken);
        return entity is null ? null : CostingBuilder.Map(await RepairPlaceholderAsync(entity, cancellationToken));
    }

    public async Task<CostingRequestDto> CreateFromSalesOrderAsync(
        Guid salesOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await LoadOrderAsync(salesOrderId, ct);
            var existing = await _costingRequests.Query()
                .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId, ct);

            if (existing is not null
                && (existing.Status == CostingRequestStatuses.Approved
                    || (!CostingBuilder.IsSellingPricePlaceholder(existing)
                        && !IsEmptyJsonArray(existing.EstimationProductLinesJson)
                        && existing.CoatingStatus != CoatingStatuses.Pending)))
            {
                return CostingBuilder.Map(existing);
            }

            var number = existing?.Number
                ?? await DocumentNumberGenerator.NextAsync(_sequences, DocumentSequenceTypes.CostingRequest, ct);
            var resolve = await ProductCostCatalog.LoadAsync(_products, order.Lines.Select(line => line.ProductId), ct);
            var built = CostingBuilder.BuildFromSalesOrder(order, number, existing, resolve);

            if (existing is null)
            {
                existing = CostingRequest.Create(
                    built.RequestNumber,
                    order.Id,
                    order.Number,
                    order.QuotationId,
                    order.QuotationNumber,
                    order.CustomerName,
                    $"SO {order.Number}",
                    built.TotalEstimate,
                    built.ProposedPrice,
                    built.MarginPercent,
                    order.Currency,
                    "Net 30",
                    built.LineItemsJson,
                    built.CoatingItemsJson,
                    built.EstimationMaterialsJson,
                    built.EstimationProductLinesJson,
                    built.RequesterJson,
                    built.ApprovalLevelsJson,
                    built.HistoryJson,
                    built.Status,
                    built.CoatingStatus,
                    built.ConfigSnapshotJson);
                await _costingRequests.AddAsync(existing, ct);
                order.SetCostingRequest(existing.Id);
            }
            else
            {
                existing.SyncContent(
                    built.TotalEstimate,
                    built.ProposedPrice,
                    built.MarginPercent,
                    built.LineItemsJson,
                    built.CoatingItemsJson,
                    built.EstimationMaterialsJson,
                    built.EstimationProductLinesJson,
                    built.Status,
                    built.CoatingStatus,
                    built.HistoryJson);
                order.SetCostingRequest(existing.Id);
            }

            return CostingBuilder.Map(existing);
        }, cancellationToken);
    }

    public async Task<CostingRequestDto> SyncFromSalesOrderAsync(
        Guid salesOrderId,
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var order = await LoadOrderAsync(salesOrderId, ct);
            var existing = await _costingRequests.Query()
                .FirstOrDefaultAsync(x => x.SalesOrderId == salesOrderId, ct);

            if (existing is not null
                && (existing.Status == CostingRequestStatuses.Approved
                    || existing.Status == CostingRequestStatuses.Rejected))
            {
                return CostingBuilder.Map(existing);
            }

            var number = existing?.Number
                ?? await DocumentNumberGenerator.NextAsync(_sequences, DocumentSequenceTypes.CostingRequest, ct);
            var resolve = await ProductCostCatalog.LoadAsync(_products, order.Lines.Select(line => line.ProductId), ct);
            var built = CostingBuilder.BuildFromSalesOrder(order, number, existing, resolve);

            if (existing is null)
            {
                existing = CostingRequest.Create(
                    built.RequestNumber,
                    order.Id,
                    order.Number,
                    order.QuotationId,
                    order.QuotationNumber,
                    order.CustomerName,
                    $"SO {order.Number}",
                    built.TotalEstimate,
                    built.ProposedPrice,
                    built.MarginPercent,
                    order.Currency,
                    "Net 30",
                    built.LineItemsJson,
                    built.CoatingItemsJson,
                    built.EstimationMaterialsJson,
                    built.EstimationProductLinesJson,
                    built.RequesterJson,
                    built.ApprovalLevelsJson,
                    built.HistoryJson,
                    built.Status,
                    built.CoatingStatus,
                    built.ConfigSnapshotJson);
                await _costingRequests.AddAsync(existing, ct);
                order.SetCostingRequest(existing.Id);
            }
            else
            {
                existing.SyncContent(
                    built.TotalEstimate,
                    built.ProposedPrice,
                    built.MarginPercent,
                    built.LineItemsJson,
                    built.CoatingItemsJson,
                    built.EstimationMaterialsJson,
                    built.EstimationProductLinesJson,
                    built.Status,
                    built.CoatingStatus,
                    built.HistoryJson);
            }

            return CostingBuilder.Map(existing);
        }, cancellationToken);
    }

    public async Task<CostingRequestDto> SubmitCoatingAsync(
        SubmitCoatingCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var entity = await _costingRequests.Query()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Costing request '{command.Id}' was not found.");

        if (entity.Status is CostingRequestStatuses.Approved or CostingRequestStatuses.Rejected)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.Id),
                    "Approved or rejected costing is kept as a historical snapshot and cannot be recalculated.",
                    ValidationErrorCodes.InvalidState),
            ]);
        }

        var submitted = CostingBuilder.ApplySubmission(entity, command.Items, command.Materials);
        var history = PrefixedHistory(entity.HistoryJson, "Coating submitted", command.ActorName ?? "System", command.Notes);
        var status = entity.Status is CostingRequestStatuses.Pending or CostingRequestStatuses.ChangesRequested
            ? CostingRequestStatuses.InReview
            : entity.Status;

        entity.SubmitCoating(
            submitted.CoatingItemsJson,
            submitted.MaterialsJson,
            submitted.LineItemsJson,
            submitted.TotalEstimate,
            submitted.MarginPercent,
            status,
            history,
            null);
        if (submitted.ProductLinesJson != entity.EstimationProductLinesJson)
        {
            entity.SyncContent(
                entity.TotalEstimate,
                entity.ProposedPrice,
                entity.MarginPercent,
                entity.LineItemsJson,
                entity.CoatingItemsJson,
                entity.EstimationMaterialsJson,
                submitted.ProductLinesJson,
                entity.Status,
                entity.CoatingStatus,
                entity.HistoryJson);
        }

        if (!string.IsNullOrWhiteSpace(command.Notes))
        {
            entity.UpdateNotes(command.Notes);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CostingBuilder.Map(entity);
    }

    public async Task<CostingRequestDto> ApproveAsync(
        CostingDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        return await ApplyDecisionAsync(command, CostingRequestStatuses.Approved, "Approved", cancellationToken);
    }

    public async Task<CostingRequestDto> RejectAsync(
        CostingDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Comment))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Comment), "Reject comment is required.", "Validation"),
            ]);
        }

        return await ApplyDecisionAsync(command, CostingRequestStatuses.Rejected, "Rejected", cancellationToken);
    }

    public async Task<CostingRequestDto> RequestChangesAsync(
        CostingDecisionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        if (string.IsNullOrWhiteSpace(command.Comment))
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(nameof(command.Comment), "Change request comment is required.", "Validation"),
            ]);
        }

        return await ApplyDecisionAsync(command, CostingRequestStatuses.ChangesRequested, "Changes requested", cancellationToken);
    }

    public async Task<CostingRequestDto> UpdateNotesAsync(
        UpdateCostingNotesCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var entity = await _costingRequests.Query()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Costing request '{command.Id}' was not found.");

        entity.UpdateNotes(command.Notes);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CostingBuilder.Map(entity);
    }

    public async Task<CostingRequestDto> AddCommentAsync(
        CostingCommentCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);
        var entity = await _costingRequests.Query()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Costing request '{command.Id}' was not found.");

        var comments = JsonColumn.Deserialize(entity.CommentsJson, new List<object>());
        comments.Insert(0, new
        {
            id = Guid.NewGuid(),
            comment = command.Comment,
            userName = command.ActorName ?? "System",
            timestamp = DateTimeOffset.UtcNow,
        });
        entity.SetComments(JsonColumn.Serialize(comments));
        entity.SetStatus(
            entity.Status,
            PrefixedHistory(entity.HistoryJson, "Comment added", command.ActorName ?? "System", command.Comment));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CostingBuilder.Map(entity);
    }

    private async Task<CostingRequestDto> ApplyDecisionAsync(
        CostingDecisionCommand command,
        string status,
        string action,
        CancellationToken cancellationToken)
    {
        var entity = await _costingRequests.Query()
            .FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException($"Costing request '{command.Id}' was not found.");

        var levelsElement = JsonColumn.ParseElement(entity.ApprovalLevelsJson);
        string? approvalJson = entity.ApprovalLevelsJson;
        if (levelsElement.HasValue && levelsElement.Value.ValueKind == JsonValueKind.Array)
        {
            var updated = new List<object>();
            var decided = false;
            foreach (var level in levelsElement.Value.EnumerateArray())
            {
                var levelStatus = level.TryGetProperty("status", out var s) ? s.GetString() : "waiting";
                if (!decided && levelStatus == "pending")
                {
                    updated.Add(new
                    {
                        id = level.TryGetProperty("id", out var id) ? id.GetString() : Guid.NewGuid().ToString(),
                        role = level.TryGetProperty("role", out var role) ? role.GetString() : "",
                        assigneeName = level.TryGetProperty("assigneeName", out var an) ? an.GetString() : "",
                        status = status == CostingRequestStatuses.Approved ? "approved"
                            : status == CostingRequestStatuses.Rejected ? "rejected" : "pending",
                    });
                    decided = true;
                    continue;
                }

                updated.Add(JsonSerializer.Deserialize<object>(level.GetRawText(), JsonColumn.Options)!);
            }

            if (status == CostingRequestStatuses.Approved && decided)
            {
                // Promote next waiting to pending
                for (var i = 0; i < updated.Count; i++)
                {
                    var raw = JsonSerializer.SerializeToElement(updated[i], JsonColumn.Options);
                    if (raw.TryGetProperty("status", out var st) && st.GetString() == "waiting")
                    {
                        updated[i] = new
                        {
                            id = raw.TryGetProperty("id", out var id) ? id.GetString() : Guid.NewGuid().ToString(),
                            role = raw.TryGetProperty("role", out var role) ? role.GetString() : "",
                            assigneeName = raw.TryGetProperty("assigneeName", out var an) ? an.GetString() : "",
                            status = "pending",
                        };
                        // If more levels remain pending, keep overall in_review
                        if (status == CostingRequestStatuses.Approved)
                        {
                            status = CostingRequestStatuses.InReview;
                        }
                        break;
                    }
                }

                // If all approved, mark approved
                var allApproved = updated.All(item =>
                {
                    var el = JsonSerializer.SerializeToElement(item, JsonColumn.Options);
                    return el.TryGetProperty("status", out var st) && st.GetString() == "approved";
                });
                if (allApproved)
                {
                    status = CostingRequestStatuses.Approved;
                }
            }

            approvalJson = JsonColumn.Serialize(updated);
        }

        entity.SetStatus(
            status,
            PrefixedHistory(entity.HistoryJson, action, command.ActorName ?? "System", command.Comment),
            approvalJson);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return CostingBuilder.Map(entity);
    }

    private async Task<CostingRequest> RepairPlaceholderAsync(CostingRequest entity, CancellationToken cancellationToken)
    {
        if (!CostingBuilder.IsSellingPricePlaceholder(entity))
        {
            return entity;
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var tracked = await _costingRequests.Query()
                .FirstOrDefaultAsync(x => x.Id == entity.Id, ct);
            if (tracked is null || !CostingBuilder.IsSellingPricePlaceholder(tracked))
            {
                return tracked ?? entity;
            }

            var order = await LoadOrderAsync(tracked.SalesOrderId, ct);
            var resolve = await ProductCostCatalog.LoadAsync(_products, order.Lines.Select(line => line.ProductId), ct);
            var built = CostingBuilder.BuildFromSalesOrder(order, tracked.Number, tracked, resolve);
            tracked.SyncContent(
                built.TotalEstimate,
                built.ProposedPrice,
                built.MarginPercent,
                built.LineItemsJson,
                built.CoatingItemsJson,
                built.EstimationMaterialsJson,
                built.EstimationProductLinesJson,
                built.Status,
                built.CoatingStatus,
                built.HistoryJson);
            return tracked;
        }, cancellationToken);
    }

    private async Task<SalesOrder> LoadOrderAsync(Guid salesOrderId, CancellationToken cancellationToken)
    {
        return await _salesOrders.Query()
            .Include(x => x.Lines)
            .FirstOrDefaultAsync(x => x.Id == salesOrderId, cancellationToken)
            ?? throw new NotFoundException($"Sales order '{salesOrderId}' was not found.");
    }

    private static bool IsEmptyJsonArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return true;
        var element = JsonColumn.ParseElement(json);
        return element is null || element.Value.ValueKind != JsonValueKind.Array || element.Value.GetArrayLength() == 0;
    }

    private static string PrefixedHistory(string historyJson, string action, string userName, string? comment)
    {
        var history = JsonColumn.Deserialize(historyJson, new List<object>());
        history.Insert(0, new
        {
            id = Guid.NewGuid(),
            action,
            userName,
            timestamp = DateTimeOffset.UtcNow,
            comment,
        });
        return JsonColumn.Serialize(history);
    }
}
