namespace PeriodClose.Domain.Common;

public static class PeriodStatuses
{
    public const string Open = "open";
    public const string Closing = "closing";
    public const string Closed = "closed";
    public const string Reopened = "reopened";

    public static bool IsWritable(string status) =>
        status is Open or Reopened;

    public static bool IsLocked(string status) =>
        status is Closed or Closing;

    public static bool CanClose(string status) =>
        status is Open or Reopened;
}

public static class PeriodTypes
{
    public const string Day = "day";
    public const string Month = "month";
}

public static class WorkerSessionCloseRules
{
    public const string PauseAndCheckpoint = "pause_and_checkpoint";
    public const string AllowCrossDate = "allow_cross_date";
    public const string RequireSupervisorConfirm = "require_supervisor_confirm";
}

public static class PeriodCloseDefaults
{
    public const string DefaultBranchId = "default";
    public const string SystemUserId = "system";
    public const string SystemUserName = "System";
    public const int RequiredDailyWorkMinutes = 480;
    public const int DoubleOvertimeAfterMinutes = 600;
    public const int FiscalYearStartMonth = 1;
}

public static class PeriodCloseErrorCodes
{
    public const string PeriodLocked = "PERIOD_LOCKED";
    public const string MonthLocked = "MONTH_LOCKED";
    public const string PeriodClosing = "PERIOD_CLOSING";
    public const string InvalidState = "INVALID_STATE";
    public const string ValidationFailed = "VALIDATION_FAILED";
}
