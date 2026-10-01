namespace PeriodClose.Application.PeriodClose;

public sealed record EmployeeWorkSessionDto(
    Guid Id,
    string EmployeeId,
    string EmployeeName,
    string BusinessDate,
    string? TaskId,
    string? TaskName,
    string? ProductionOrderId,
    string? ProductionOrderNumber,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    string Status,
    int WorkedMinutes,
    int PauseMinutes,
    int BreakMinutes);
