namespace Sales.Domain.Common;

public static class QuotationStatuses
{
    public const string Draft = "draft";
    public const string ReadyToSend = "ready_to_send";
    public const string Sent = "sent";
    public const string Viewed = "viewed";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Expired = "expired";
    public const string Converted = "converted";
}

public static class SalesOrderStatuses
{
    public const string Draft = "draft";
    public const string PendingReview = "pending_review";
    public const string Submitted = "submitted";
    public const string Confirmed = "confirmed";
    public const string InManufacturing = "in_manufacturing";
    public const string ReadyForDelivery = "ready_for_delivery";
    public const string PartiallyDelivered = "partially_delivered";
    public const string Delivered = "delivered";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";

    public static bool IsConfirmable(string status) =>
        status is Draft or PendingReview or Submitted;

    public static bool IsDeletable(string status) =>
        status is Draft or PendingReview;
}

public static class CostingRequestStatuses
{
    public const string Pending = "pending";
    public const string InReview = "in_review";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
    public const string ChangesRequested = "changes_requested";
}

public static class CoatingStatuses
{
    public const string Pending = "pending";
    public const string Submitted = "submitted";
    public const string Skipped = "skipped";
}

public static class PaymentStatuses
{
    public const string Unpaid = "unpaid";
    public const string Partial = "partial";
    public const string Paid = "paid";
    public const string Overdue = "overdue";
}

public static class PriorityValues
{
    public const string Low = "low";
    public const string Medium = "medium";
    public const string High = "high";
    public const string Urgent = "urgent";
}

public static class DocumentSequenceTypes
{
    public const string Quotation = "Q";
    public const string SalesOrder = "SO";
    public const string CostingRequest = "CR";
    public const string ManufacturingJob = "MJ";
    public const string Delivery = "DL";
    public const string NumberFormat = "{0}-{1}-{2:D4}";
}

public static class SalesDefaults
{
    public const string Currency = "LKR";
    public const string SystemActor = "system";
    public const string SystemActorName = "System";
    public const string PaymentTerms = "Net 30";
    public const string SalesOrderReferenceType = "sales_order";
    public const string CostingRequestType = "Sales Order Costing";
    public const string DefaultSlaRemaining = "48h";
    public const string SalesOrderTitleFormat = "SO {0}";
    public const string ReservationNoteFormat = "Reservation for {0}";
}

public static class SalesJsonFields
{
    public const string IsLocked = "isLocked";
    public const string SalesOrderLineItemId = "salesOrderLineItemId";
    public const string InventoryItemId = "inventoryItemId";
    public const string RequiredQuantity = "requiredQuantity";
    public const string Quantity = "quantity";
}

public static class CustomizationStatuses
{
    public const string Draft = "draft";
    public const string PendingApproval = "pending_approval";
    public const string Approved = "approved";
    public const string Rejected = "rejected";
}
