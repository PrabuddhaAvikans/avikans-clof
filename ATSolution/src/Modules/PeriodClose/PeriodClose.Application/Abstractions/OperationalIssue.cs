using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record OperationalIssue(
    string Code,
    string Type,
    string Message,
    bool IsBlocking,
    string? EntityType = null,
    string? EntityId = null);
