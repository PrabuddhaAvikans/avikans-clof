namespace ATSolution.Application.Abstractions.Audit;

/// <summary>
/// Cross-module write port for activity audit events.
/// Implemented by the Audit module; consumed by Identity and others.
/// </summary>
public interface IAuditEventWriter
{
    Task WriteAsync(AuditEventWriteRequest request, CancellationToken cancellationToken = default);
}

public sealed record AuditChangeWriteRequest(string Field, string? From = null, string? To = null);

public sealed record AuditEventWriteRequest(
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
    IReadOnlyList<AuditChangeWriteRequest>? Changes = null,
    DateTimeOffset? Timestamp = null);
