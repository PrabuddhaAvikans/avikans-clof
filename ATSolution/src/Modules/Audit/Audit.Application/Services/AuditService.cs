using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.SharedKernel.Models;
using Audit.Application.Abstractions;
using Audit.Application.AuditLogs;
using Audit.Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Audit.Application.Services;

public sealed class AuditService : IAuditService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRepository<AuditLogEntry, Guid> _logs;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public AuditService(
        IRepository<AuditLogEntry, Guid> logs,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _logs = logs;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<AuditLogEntryDto>> ListAsync(
        AuditLogListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);
        var logs = ApplyFilters(_logs.Query().AsNoTracking(), query.Entity, query.Action, query.Severity, query.UserId, query.From, query.To);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            logs = logs.Where(l =>
                l.Details.Contains(search)
                || l.UserName.Contains(search)
                || l.EntityLabel != null && l.EntityLabel.Contains(search)
                || l.EntityId.Contains(search));
        }

        logs = logs.OrderByDescending(l => l.Timestamp);
        var totalCount = await logs.CountAsync(cancellationToken);
        var items = await logs.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return PaginatedResponse<AuditLogEntryDto>.Create(items.Select(Map).ToList(), totalCount, page, pageSize);
    }

    public async Task<AuditLogEntryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entry = await _logs.Query().AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);
        return entry is null ? null : Map(entry);
    }

    public async Task<AuditLogSummaryDto> SummaryAsync(
        AuditLogSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        var logs = ApplyFilters(_logs.Query().AsNoTracking(), query.Entity, query.Action, query.Severity, query.UserId, query.From, query.To);
        var todayStart = new DateTimeOffset(DateTimeOffset.UtcNow.Year, DateTimeOffset.UtcNow.Month, DateTimeOffset.UtcNow.Day, 0, 0, 0, TimeSpan.Zero);

        var total = await logs.CountAsync(cancellationToken);
        var info = await logs.CountAsync(l => l.Severity == AuditSeverities.Info, cancellationToken);
        var warning = await logs.CountAsync(l => l.Severity == AuditSeverities.Warning, cancellationToken);
        var critical = await logs.CountAsync(l => l.Severity == AuditSeverities.Critical, cancellationToken);
        var today = await logs.CountAsync(l => l.Timestamp >= todayStart, cancellationToken);

        return new AuditLogSummaryDto(total, info, warning, critical, today);
    }

    public async Task<AuditLogEntryDto> AppendAsync(
        AppendAuditLogCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var changesJson = command.Changes is null || command.Changes.Count == 0
            ? null
            : JsonSerializer.Serialize(command.Changes, JsonOptions);

        var entry = AuditLogEntry.Create(
            command.Timestamp ?? DateTimeOffset.UtcNow,
            command.UserId,
            command.UserName,
            command.Action,
            command.Entity,
            command.EntityId,
            command.EntityLabel,
            command.Details,
            command.Severity,
            command.IpAddress,
            command.UserAgent,
            changesJson);

        await _logs.AddAsync(entry, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(entry);
    }

    private static IQueryable<AuditLogEntry> ApplyFilters(
        IQueryable<AuditLogEntry> logs,
        string? entity,
        string? action,
        string? severity,
        string? userId,
        DateTimeOffset? from,
        DateTimeOffset? to)
    {
        if (!string.IsNullOrWhiteSpace(entity))
            logs = logs.Where(l => l.Entity == entity);
        if (!string.IsNullOrWhiteSpace(action))
            logs = logs.Where(l => l.Action == action);
        if (!string.IsNullOrWhiteSpace(severity))
            logs = logs.Where(l => l.Severity == severity);
        if (!string.IsNullOrWhiteSpace(userId))
            logs = logs.Where(l => l.UserId == userId);
        if (from.HasValue)
            logs = logs.Where(l => l.Timestamp >= from.Value);
        if (to.HasValue)
            logs = logs.Where(l => l.Timestamp <= to.Value);
        return logs;
    }

    private static AuditLogEntryDto Map(AuditLogEntry entry)
    {
        IReadOnlyList<AuditChangeDto>? changes = null;
        if (!string.IsNullOrWhiteSpace(entry.ChangesJson))
        {
            try
            {
                changes = JsonSerializer.Deserialize<List<AuditChangeDto>>(entry.ChangesJson, JsonOptions);
            }
            catch
            {
                changes = null;
            }
        }

        return new AuditLogEntryDto(
            entry.Id,
            entry.Timestamp.ToString("O"),
            entry.UserId,
            entry.UserName,
            entry.Action,
            entry.Entity,
            entry.EntityId,
            entry.EntityLabel,
            entry.Details,
            entry.Severity,
            entry.IpAddress,
            entry.UserAgent,
            changes);
    }
}
