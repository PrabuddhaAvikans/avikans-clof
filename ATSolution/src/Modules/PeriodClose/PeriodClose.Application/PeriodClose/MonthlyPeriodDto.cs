namespace PeriodClose.Application.PeriodClose;

public sealed record MonthlyPeriodDto(
    Guid Id,
    string BranchId,
    int Year,
    int Month,
    string Status,
    DateTimeOffset StartedAt,
    string StartedBy,
    string StartedByName,
    DateTimeOffset? ClosedAt,
    string? ClosedBy,
    string? ClosedByName,
    DateTimeOffset? ReopenedAt,
    string? ReopenedBy,
    string? ReopenedByName,
    string? ReopenReason,
    DateTimeOffset? OriginalClosedAt,
    string? OriginalClosedBy,
    string? OriginalClosedByName,
    int CloseCount);
