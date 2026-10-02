namespace Sales.Domain.Common;

public static class QuotationStatuses
{
    public const string Draft = "draft";
    public const string ReadyToSend = "ready_to_send";
    public const string Sent = "sent";
    public const string Viewed = "viewed";
    public const string CustomerFeedback = "customer_feedback";
    public const string RevisionRequired = "revision_required";
    public const string Revised = "revised";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string Expired = "expired";
    public const string Converted = "converted";

    public static bool IsKnown(string status) =>
        status is Draft or ReadyToSend or Sent or Viewed or CustomerFeedback
            or RevisionRequired or Revised or Accepted or Rejected or Expired or Converted;

    public static bool IsTerminal(string status) =>
        status is Converted or Rejected or Expired;

    public static bool IsEditable(string status) =>
        status is Draft or ReadyToSend or Sent or Viewed or CustomerFeedback
            or RevisionRequired or Revised or Accepted;

    public static bool IsDeletable(string status) =>
        status is Draft or ReadyToSend;

    public static bool CanSend(string status) =>
        status is Draft or ReadyToSend or Revised or RevisionRequired
            or CustomerFeedback or Viewed or Sent;

    public static bool CanConvert(string status) =>
        status is Accepted;

    public static bool CanLogContact(string status) =>
        IsKnown(status);

    /// <summary>
    /// Issued to the customer (or later). Content edits create a new revision.
    /// </summary>
    public static bool IsIssuedOrLater(string status) =>
        status is Sent or Viewed or CustomerFeedback or RevisionRequired
            or Revised or Accepted;

    public static bool ShouldRecordCustomerFeedback(string status) =>
        status is Sent or Viewed;

    public static bool CanTransitionTo(string from, string to)
    {
        if (from == to) return true;
        if (IsTerminal(from)) return false;
        if (!IsKnown(to)) return false;

        return to switch
        {
            ReadyToSend => from is Draft,
            Sent => from is Draft or ReadyToSend or Revised or RevisionRequired
                or CustomerFeedback or Viewed or Sent,
            Viewed => from is Sent,
            CustomerFeedback => from is Sent or Viewed or CustomerFeedback,
            RevisionRequired => from is Sent or Viewed or CustomerFeedback
                or Revised or Accepted or RevisionRequired,
            Revised => from is Sent or Viewed or CustomerFeedback or RevisionRequired
                or Accepted or Revised,
            Accepted => from is Sent or Viewed or CustomerFeedback or RevisionRequired
                or Revised or Accepted,
            Rejected => from is Sent or Viewed or CustomerFeedback or RevisionRequired
                or Revised or ReadyToSend,
            Converted => from is Accepted,
            Expired => !IsTerminal(from),
            Draft => false,
            _ => false,
        };
    }

    public static string ResolveStatusAfterContentSave(string currentStatus, string? saveMode)
    {
        if (string.Equals(saveMode, "draft", StringComparison.OrdinalIgnoreCase)
            && IsIssuedOrLater(currentStatus)
            && currentStatus is not Revised)
        {
            return RevisionRequired;
        }

        if (string.Equals(saveMode, "save", StringComparison.OrdinalIgnoreCase))
        {
            if (currentStatus == Draft)
                return ReadyToSend;

            if (IsIssuedOrLater(currentStatus) && currentStatus != Revised)
                return Revised;
        }

        return currentStatus;
    }
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

    /// <summary>
    /// Confirmed (or later operational) orders can start manufacturing.
    /// </summary>
    public static bool CanManufacture(string status) =>
        status is Confirmed or InManufacturing or ReadyForDelivery or PartiallyDelivered;

    /// <summary>
    /// Confirmed (or later) orders can create deliveries.
    /// </summary>
    public static bool CanDeliver(string status) =>
        status is Confirmed or InManufacturing or ReadyForDelivery or PartiallyDelivered;

    /// <summary>
    /// Block cancel after delivery progress or terminal completion.
    /// </summary>
    public static bool IsCancellable(string status) =>
        status is not Cancelled and not PartiallyDelivered and not Delivered and not Completed;
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
