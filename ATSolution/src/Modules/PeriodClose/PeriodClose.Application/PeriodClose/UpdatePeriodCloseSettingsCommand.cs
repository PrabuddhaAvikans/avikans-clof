using ATSolution.SharedKernel.Models;

namespace PeriodClose.Application.PeriodClose;

public sealed record UpdatePeriodCloseSettingsCommand(
    string BranchId,
    string? WorkerSessionCloseRule = null,
    bool? AllowNegativeStock = null,
    bool? RequireAllDaysClosedForMonthlyClose = null,
    int? FiscalYearStartMonth = null,
    int? RequiredDailyWorkMinutes = null,
    bool? AllowIncompleteEmployeeHoursException = null,
    bool? CountPauseAsWorked = null,
    int? DoubleOvertimeAfterMinutes = null,
    bool? RequireOvertimeApproval = null);
