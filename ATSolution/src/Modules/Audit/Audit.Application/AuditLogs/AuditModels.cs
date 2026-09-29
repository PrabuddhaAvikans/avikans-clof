using ATSolution.SharedKernel.Models;

namespace Audit.Application.AuditLogs;

public sealed class AuditLogListQuery : PaginatedRequest
{
    public string? Entity { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed class AuditLogSummaryQuery
{
    public string? Entity { get; set; }
    public string? Action { get; set; }
    public string? Severity { get; set; }
    public string? UserId { get; set; }
    public DateTimeOffset? From { get; set; }
    public DateTimeOffset? To { get; set; }
}

public sealed record AuditChangeDto(string Field, string? From = null, string? To = null);

public sealed record AuditLogEntryDto(
    Guid Id,
    string Timestamp,
    string UserId,
    string UserName,
    string Action,
    string Entity,
    string EntityId,
    string? EntityLabel,
    string Details,
    string Severity,
    string? IpAddress,
    string? UserAgent,
    IReadOnlyList<AuditChangeDto>? Changes);

public sealed record AuditLogSummaryDto(
    int Total,
    int Info,
    int Warning,
    int Critical,
    int Today);

public sealed record AppendAuditLogCommand(
    string UserId,
    string UserName,
    string Action,
    string Entity,
    string EntityId,
    string Details,
    string Severity = "info",
    string? EntityLabel = null,
    string? IpAddress = null,
    string? UserAgent = null,
    IReadOnlyList<AuditChangeDto>? Changes = null,
    DateTimeOffset? Timestamp = null);
