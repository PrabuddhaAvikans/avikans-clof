namespace PeriodClose.Application.PeriodClose;

public sealed record EmployeeTaskWorkBreakdownDto(
    string TaskId,
    string TaskName,
    string? ProductionOrderId,
    string? ProductionOrderNumber,
    int WorkedMinutes);
