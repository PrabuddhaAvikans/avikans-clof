using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Settings;

public class PeriodCloseSettings : Entity<Guid>, IAuditableEntity
{
    public string BranchId { get; private set; } = PeriodCloseDefaults.DefaultBranchId;
    public string WorkerSessionCloseRule { get; private set; } = WorkerSessionCloseRules.PauseAndCheckpoint;
    public bool AllowNegativeStock { get; private set; }
    public bool RequireAllDaysClosedForMonthlyClose { get; private set; } = true;
    public int FiscalYearStartMonth { get; private set; } = PeriodCloseDefaults.FiscalYearStartMonth;
    public int RequiredDailyWorkMinutes { get; private set; } = PeriodCloseDefaults.RequiredDailyWorkMinutes;
    public bool AllowIncompleteEmployeeHoursException { get; private set; }
    public bool CountPauseAsWorked { get; private set; }
    public int DoubleOvertimeAfterMinutes { get; private set; } = PeriodCloseDefaults.DoubleOvertimeAfterMinutes;
    public bool RequireOvertimeApproval { get; private set; } = true;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static PeriodCloseSettings CreateDefault(string branchId)
    {
        var now = DateTimeOffset.UtcNow;
        return new PeriodCloseSettings
        {
            Id = Guid.NewGuid(),
            BranchId = NormalizeBranch(branchId),
            WorkerSessionCloseRule = WorkerSessionCloseRules.PauseAndCheckpoint,
            AllowNegativeStock = false,
            RequireAllDaysClosedForMonthlyClose = true,
            FiscalYearStartMonth = PeriodCloseDefaults.FiscalYearStartMonth,
            RequiredDailyWorkMinutes = PeriodCloseDefaults.RequiredDailyWorkMinutes,
            AllowIncompleteEmployeeHoursException = false,
            CountPauseAsWorked = false,
            DoubleOvertimeAfterMinutes = PeriodCloseDefaults.DoubleOvertimeAfterMinutes,
            RequireOvertimeApproval = true,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? workerSessionCloseRule,
        bool? allowNegativeStock,
        bool? requireAllDaysClosedForMonthlyClose,
        int? fiscalYearStartMonth,
        int? requiredDailyWorkMinutes,
        bool? allowIncompleteEmployeeHoursException,
        bool? countPauseAsWorked,
        int? doubleOvertimeAfterMinutes,
        bool? requireOvertimeApproval)
    {
        if (workerSessionCloseRule is not null) WorkerSessionCloseRule = workerSessionCloseRule;
        if (allowNegativeStock.HasValue) AllowNegativeStock = allowNegativeStock.Value;
        if (requireAllDaysClosedForMonthlyClose.HasValue)
            RequireAllDaysClosedForMonthlyClose = requireAllDaysClosedForMonthlyClose.Value;
        if (fiscalYearStartMonth.HasValue) FiscalYearStartMonth = fiscalYearStartMonth.Value;
        if (requiredDailyWorkMinutes.HasValue) RequiredDailyWorkMinutes = requiredDailyWorkMinutes.Value;
        if (allowIncompleteEmployeeHoursException.HasValue)
            AllowIncompleteEmployeeHoursException = allowIncompleteEmployeeHoursException.Value;
        if (countPauseAsWorked.HasValue) CountPauseAsWorked = countPauseAsWorked.Value;
        if (doubleOvertimeAfterMinutes.HasValue) DoubleOvertimeAfterMinutes = doubleOvertimeAfterMinutes.Value;
        if (requireOvertimeApproval.HasValue) RequireOvertimeApproval = requireOvertimeApproval.Value;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private static string NormalizeBranch(string? branchId) =>
        string.IsNullOrWhiteSpace(branchId)
            ? PeriodCloseDefaults.DefaultBranchId
            : branchId.Trim();
}
