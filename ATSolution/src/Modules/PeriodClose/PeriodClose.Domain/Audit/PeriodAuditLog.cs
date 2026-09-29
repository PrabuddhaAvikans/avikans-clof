using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Audit;

public class PeriodAuditLog : Entity<Guid>
{
    public string PeriodType { get; private set; } = PeriodTypes.Day;
    public Guid PeriodId { get; private set; }
    public string Action { get; private set; } = null!;
    public string UserId { get; private set; } = PeriodCloseDefaults.SystemUserId;
    public string UserName { get; private set; } = PeriodCloseDefaults.SystemUserName;
    public string? Reason { get; private set; }
    public string? DetailsJson { get; private set; }
    public DateTimeOffset PerformedAt { get; private set; }

    public static PeriodAuditLog Create(
        string periodType,
        Guid periodId,
        string action,
        string userId,
        string userName,
        string? reason = null,
        string? detailsJson = null,
        DateTimeOffset? performedAt = null)
    {
        return new PeriodAuditLog
        {
            Id = Guid.NewGuid(),
            PeriodType = periodType,
            PeriodId = periodId,
            Action = action,
            UserId = userId,
            UserName = userName,
            Reason = reason,
            DetailsJson = detailsJson,
            PerformedAt = performedAt ?? DateTimeOffset.UtcNow,
        };
    }
}
