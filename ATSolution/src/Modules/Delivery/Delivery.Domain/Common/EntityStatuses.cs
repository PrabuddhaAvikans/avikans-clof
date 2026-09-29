namespace Delivery.Domain.Common;

public static class DeliveryStatuses
{
    public const string Planned = "planned";
    public const string ReadyForDispatch = "ready_for_dispatch";
    public const string Dispatched = "dispatched";
    public const string InTransit = "in_transit";
    public const string PartiallyDelivered = "partially_delivered";
    public const string Delivered = "delivered";
    public const string Failed = "failed";
    public const string Returned = "returned";
    public const string Cancelled = "cancelled";

    public static bool CanTransition(string from, string to) =>
        (from, to) switch
        {
            (Planned, ReadyForDispatch) => true,
            (Planned, Cancelled) => true,
            (ReadyForDispatch, Dispatched) => true,
            (ReadyForDispatch, Planned) => true,
            (ReadyForDispatch, Cancelled) => true,
            (Dispatched, InTransit) => true,
            (Dispatched, Cancelled) => true,
            (InTransit, Delivered) => true,
            (InTransit, PartiallyDelivered) => true,
            (InTransit, Failed) => true,
            (InTransit, Returned) => true,
            (PartiallyDelivered, Delivered) => true,
            (PartiallyDelivered, Failed) => true,
            (PartiallyDelivered, Returned) => true,
            _ => false,
        };
}

public static class PriorityValues
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Urgent = "urgent";
}
