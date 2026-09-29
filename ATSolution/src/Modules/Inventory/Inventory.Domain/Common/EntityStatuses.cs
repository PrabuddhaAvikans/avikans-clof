namespace Inventory.Domain.Common;

public static class EntityStatuses
{
    public const string Active = "active";
    public const string Inactive = "inactive";
}

public static class StockMovementTypes
{
    public const string Receipt = "receipt";
    public const string Issue = "issue";
    public const string Transfer = "transfer";
    public const string Adjustment = "adjustment";
    public const string Reservation = "reservation";
    public const string Release = "release";
}

public static class StockStatuses
{
    public const string InStock = "in_stock";
    public const string LowStock = "low_stock";
    public const string OutOfStock = "out_of_stock";
    public const string Reserved = "reserved";
}

public static class ReprocessingBatchStatuses
{
    public const string Draft = "draft";
    public const string InProgress = "in_progress";
    public const string Completed = "completed";
    public const string Cancelled = "cancelled";
}

public static class InventoryItemTypes
{
    public const string ReusableScrap = "reusable_scrap";
    public const string ReprocessingWip = "reprocessing_wip";
    public const string RecoveredMaterial = "recovered_material";
}
