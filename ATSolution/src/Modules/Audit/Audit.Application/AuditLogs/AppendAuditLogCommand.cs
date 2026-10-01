using ATSolution.SharedKernel.Models;

namespace Audit.Application.AuditLogs;

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
