namespace Audit.Application.AuditLogs;

public sealed record AuditLogEntryDto(
    Guid Id,
    DateTimeOffset Timestamp,
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
