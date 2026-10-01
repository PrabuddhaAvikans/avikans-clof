namespace PeriodClose.Application.PeriodClose;

public sealed record PeriodCloseSettingsDto(
    string BranchId,
    string WorkerSessionCloseRule,
    bool AllowNegativeStock,
    bool RequireAllDaysClosedForMonthlyClose,
    int FiscalYearStartMonth,
    int RequiredDailyWorkMinutes,
    bool AllowIncompleteEmployeeHoursException,
    bool CountPauseAsWorked,
    int DoubleOvertimeAfterMinutes,
    bool RequireOvertimeApproval);
