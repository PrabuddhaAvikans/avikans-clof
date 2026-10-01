namespace PeriodClose.Application.PeriodClose;

public sealed record PeriodAuditLogDto(
    Guid Id,
    string PeriodType,
    Guid PeriodId,
    string Action,
    string UserId,
    string UserName,
    string? Reason,
    IReadOnlyDictionary<string, object?>? Details,
    DateTimeOffset PerformedAt);
