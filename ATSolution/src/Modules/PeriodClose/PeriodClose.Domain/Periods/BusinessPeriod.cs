using ATSolution.Domain.Entities.Common;
using PeriodClose.Domain.Common;

namespace PeriodClose.Domain.Periods;

public class BusinessPeriod : Entity<Guid>
{
    public string BranchId { get; private set; } = PeriodCloseDefaults.DefaultBranchId;
    public string BusinessDate { get; private set; } = null!;
    public string Status { get; private set; } = PeriodStatuses.Open;
    public DateTimeOffset OpenedAt { get; private set; }
    public string OpenedBy { get; private set; } = PeriodCloseDefaults.SystemUserId;
    public string OpenedByName { get; private set; } = PeriodCloseDefaults.SystemUserName;
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

    public static BusinessPeriod Open(
        string branchId,
        string businessDate,
        string openedBy,
        string openedByName,
        DateTimeOffset? openedAt = null)
    {
        var now = openedAt ?? DateTimeOffset.UtcNow;
        return new BusinessPeriod
        {
            Id = Guid.NewGuid(),
            BranchId = NormalizeBranch(branchId),
            BusinessDate = businessDate,
            Status = PeriodStatuses.Open,
            OpenedAt = now,
            OpenedBy = openedBy,
            OpenedByName = openedByName,
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
