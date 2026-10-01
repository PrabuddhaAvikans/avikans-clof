namespace PeriodClose.Application.PeriodClose;

public sealed record DayCloseValidationIssueDto(
    Guid Id,
    Guid BusinessPeriodId,
    string ValidationCode,
    string ValidationType,
    string Message,
    string? EntityType,
    string? EntityId,
    bool IsBlocking);
