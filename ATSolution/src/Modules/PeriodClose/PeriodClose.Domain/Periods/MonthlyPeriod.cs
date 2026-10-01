using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Periods;

public class MonthlyPeriod : Entity<Guid>
{
    public string BranchId { get; private set; } = PeriodCloseDefaults.DefaultBranchId;
    public int Year { get; private set; }
    public int Month { get; private set; }
    public string Status { get; private set; } = PeriodStatuses.Open;
    public DateTimeOffset StartedAt { get; private set; }
    public string StartedBy { get; private set; } = PeriodCloseDefaults.SystemUserId;
    public string StartedByName { get; private set; } = PeriodCloseDefaults.SystemUserName;
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? ClosedBy { get; private set; }
    public string? ClosedByName { get; private set; }
    public DateTimeOffset? ReopenedAt { get; private set; }
    public string? ReopenedBy { get; private set; }
    public string? ReopenedByName { get; private set; }
    public string? ReopenReason { get; private set; }
    public DateTimeOffset? OriginalClosedAt { get; private set; }
    public string? OriginalClosedBy { get; private set; }
    public string? OriginalClosedByName { get; private set; }
    public int CloseCount { get; private set; }

    public static MonthlyPeriod Open(
        string branchId,
        int year,
        int month,
        string startedBy,
        string startedByName,
        DateTimeOffset? startedAt = null)
    {
        var now = startedAt ?? DateTimeOffset.UtcNow;
        return new MonthlyPeriod
        {
            Id = Guid.NewGuid(),
            BranchId = NormalizeBranch(branchId),
            Year = year,
            Month = month,
            Status = PeriodStatuses.Open,
            StartedAt = now,
            StartedBy = startedBy,
            StartedByName = startedByName,
            CloseCount = 0,
        };
    }

    public void MarkClosing()
    {
        Status = PeriodStatuses.Closing;
    }

    public void MarkOpen()
    {
        Status = PeriodStatuses.Open;
        ClosedAt = null;
        ClosedBy = null;
        ClosedByName = null;
    }

    /// <summary>Puts a failed close back to the writable status it had before closing started.</summary>
    public void RestoreWritable(string previousStatus)
    {
        Status = previousStatus == PeriodStatuses.Reopened
            ? PeriodStatuses.Reopened
            : PeriodStatuses.Open;
    }

    public void Close(string closedBy, string closedByName, DateTimeOffset closedAt)
    {
        Status = PeriodStatuses.Closed;
        ClosedAt = closedAt;
        ClosedBy = closedBy;
        ClosedByName = closedByName;
        CloseCount += 1;
        OriginalClosedAt ??= closedAt;
        OriginalClosedBy ??= closedBy;
        OriginalClosedByName ??= closedByName;
        ReopenedAt = null;
        ReopenedBy = null;
        ReopenedByName = null;
        ReopenReason = null;
    }

    public void Reopen(string reopenedBy, string reopenedByName, string reason, DateTimeOffset reopenedAt)
    {
        Status = PeriodStatuses.Reopened;
        ReopenedAt = reopenedAt;
        ReopenedBy = reopenedBy;
        ReopenedByName = reopenedByName;
        ReopenReason = reason;
        ClosedAt = null;
        ClosedBy = null;
        ClosedByName = null;
    }

    private static string NormalizeBranch(string? branchId) =>
        string.IsNullOrWhiteSpace(branchId)
            ? PeriodCloseDefaults.DefaultBranchId
            : branchId.Trim();
}
