namespace PeriodClose.Application.PeriodClose;

public sealed record BusinessPeriodDto(
    Guid Id,
    string BranchId,
    string BusinessDate,
    string Status,
    DateTimeOffset OpenedAt,
    string OpenedBy,
    string OpenedByName,
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
