using PeriodClose.Application.PeriodClose;

namespace PeriodClose.Application.Abstractions;

public sealed record PeriodClosePolicy(
    int RequiredDailyWorkMinutes,
    bool CountPauseAsWorked,
    int DoubleOvertimeAfterMinutes,
    decimal LabourRatePerHour);
