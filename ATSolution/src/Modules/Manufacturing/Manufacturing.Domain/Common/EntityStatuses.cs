namespace Manufacturing.Domain.Common;

public static class ManufacturingJobStatuses
{
    public const string Draft = "draft";
    public const string Planned = "planned";
    public const string MaterialsPending = "materials_pending";
    public const string ReadyToStart = "ready_to_start";
    public const string InProgress = "in_progress";
    public const string OnHold = "on_hold";
    public const string QualityCheck = "quality_check";
    public const string Rework = "rework";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

public static class ManufacturingTaskStatuses
{
    public const string Pending = "pending";
    public const string Ready = "ready";
    public const string InProgress = "in_progress";
    public const string Paused = "paused";
    public const string Completed = "completed";
    public const string OnHold = "on_hold";
    public const string Blocked = "blocked";
    public const string Skipped = "skipped";
    public const string ReworkRequired = "rework_required";
    public const string Cancelled = "cancelled";

    public static readonly HashSet<string> Terminal =
    [
        Completed, Skipped, Cancelled
    ];

    public static readonly HashSet<string> SatisfiedPrereq =
    [
        Completed, Skipped
    ];
}

public static class TaskUnitStatuses
{
    public const string Pending = "pending";
    public const string Assigned = "assigned";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

public static class TaskUnitAssignmentStatuses
{
    public const string Assigned = "assigned";
    public const string InProgress = "in_progress";
    public const string Paused = "paused";
    public const string OnHold = "on_hold";
    public const string Completed = "completed";
}

public static class EmployeeWorkSessionStatuses
{
    public const string Started = "started";
    public const string Working = "working";
    public const string Paused = "paused";
    public const string OnHold = "on_hold";
    public const string Completed = "completed";
    public const string Stopped = "stopped";
}

public static class MaterialRequirementStatuses
{
    public const string Pending = "pending";
    public const string Reserved = "reserved";
    public const string Partial = "partial";
    public const string Issued = "issued";
}

public static class PriorityValues
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Urgent = "urgent";
}
