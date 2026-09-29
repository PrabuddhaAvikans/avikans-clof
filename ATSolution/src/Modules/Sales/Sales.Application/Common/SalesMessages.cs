namespace Sales.Application.Common;

public static class SalesMessages
{
    public const string CustomerNotFound = "Customer '{0}' was not found.";
    public const string SalesOrderNotFound = "Sales order '{0}' was not found.";
    public const string UserNotFound = "User '{0}' was not found.";
    public const string OnlyOpenOrdersCanBeUpdated = "Only open sales orders can be updated.";
    public const string OnlyDeletableOrders = "Only draft or pending-review orders can be deleted.";
    public const string OrderCannotBeConfirmed = "Sales order cannot be confirmed in its current status.";
    public const string CostingRequiredBeforeConfirm = "Create estimation and costing for this sales order first.";
    public const string EstimationStillPending = "Product estimation is still pending. Submit it, or regenerate BOM from the sales order.";
    public const string CoatingMustBeSubmittedOrSkipped = "Coating must be submitted or skipped before confirm.";
    public const string CostingMustBeApproved = "Costing must be approved before confirming this order.";
}

public static class SalesValidationFields
{
    public const string Status = "status";
    public const string Costing = "costing";
    public const string CoatingStatus = "coatingStatus";
}
