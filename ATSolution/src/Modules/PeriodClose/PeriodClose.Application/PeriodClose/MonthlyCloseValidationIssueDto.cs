namespace PeriodClose.Application.PeriodClose;

public sealed record MonthlyCloseValidationIssueDto(
    Guid Id,
    Guid MonthlyPeriodId,
    string ValidationCode,
    string ValidationType,
    string Message,
    string? EntityType,
    string? EntityId,
    bool IsBlocking);
