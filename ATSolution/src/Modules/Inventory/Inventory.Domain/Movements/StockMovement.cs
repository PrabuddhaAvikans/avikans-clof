using ATSolution.Domain.Entities.Common;

namespace Inventory.Domain.Movements;

public class StockMovement : Entity<Guid>
{
    public Guid InventoryItemId { get; private set; }
    public Items.InventoryItem InventoryItem { get; private set; } = null!;
    public string InventoryItemName { get; private set; } = null!;
    public string InventoryItemSku { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public decimal Quantity { get; private set; }
    public string Unit { get; private set; } = null!;
    public string? ReferenceType { get; private set; }
    public string? ReferenceId { get; private set; }
    public string? Notes { get; private set; }
    public string PerformedBy { get; private set; } = null!;
    public string PerformedByName { get; private set; } = null!;
    public DateTimeOffset PerformedAtUtc { get; private set; }
    public string? TraceJson { get; private set; }

    public static StockMovement Create(
        Guid inventoryItemId,
        string inventoryItemName,
        string inventoryItemSku,
        string type,
        decimal quantity,
        string unit,
        string? referenceType,
        string? referenceId,
        string? notes,
        string performedBy,
        string performedByName,
        string? traceJson)
    {
        return new StockMovement
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            InventoryItemName = inventoryItemName,
            InventoryItemSku = inventoryItemSku,
            Type = type,
            Quantity = quantity,
            Unit = unit,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Notes = notes,
            PerformedBy = performedBy,
            PerformedByName = performedByName,
            PerformedAtUtc = DateTimeOffset.UtcNow,
            TraceJson = traceJson,
        };
    }
}
