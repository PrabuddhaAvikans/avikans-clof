using ATSolution.Domain.Entities.Common;

namespace Audit.Domain.AuditLogs;

public class AuditLogEntry : Entity<Guid>
{
    public DateTimeOffset Timestamp { get; private set; }
    public string UserId { get; private set; } = null!;
    public string UserName { get; private set; } = null!;
    public string Action { get; private set; } = null!;
    public string Entity { get; private set; } = null!;
    public string EntityId { get; private set; } = null!;
    public string? EntityLabel { get; private set; }
    public string Details { get; private set; } = null!;
    public string Severity { get; private set; } = AuditSeverities.Info;
    public string? IpAddress { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ChangesJson { get; private set; }

    public static AuditLogEntry Create(
        DateTimeOffset timestamp,
        string userId,
        string userName,
        string action,
        string entity,
        string entityId,
        string? entityLabel,
        string details,
        string severity,
        string? ipAddress = null,
        string? userAgent = null,
        string? changesJson = null)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = timestamp,
            UserId = userId,
            UserName = userName,
            Action = action,
            Entity = entity,
            EntityId = entityId,
            EntityLabel = entityLabel,
            Details = details,
            Severity = severity,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            ChangesJson = changesJson,
        };
    }
}

public static class AuditSeverities
{
    public const string Info = "info";
    public const string Warning = "warning";
    public const string Critical = "critical";
}
