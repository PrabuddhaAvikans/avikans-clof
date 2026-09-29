using ATSolution.Domain.Entities.Common;

namespace Sales.Domain.SalesOrders;

public class SalesOrderLine : Entity<Guid>
{
    public Guid SalesOrderId { get; private set; }
    public SalesOrder SalesOrder { get; private set; } = null!;
    public Guid? ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public Guid? ProductVersionId { get; private set; }
    public string? ProductVersionLabel { get; private set; }
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal DiscountPercent { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public decimal QuantityDelivered { get; private set; }
    public decimal QuantityInManufacturing { get; private set; }
    public bool IsCustomized { get; private set; }
    public bool RequiresManufacturing { get; private set; }
    public string? CustomizationJson { get; private set; }
    public string? BomSnapshotJson { get; private set; }
    public string? CostSnapshotJson { get; private set; }
    public int SortOrder { get; private set; }

    public static SalesOrderLine Create(
        Guid salesOrderId,
        Guid? productId,
        string productSku,
        string productName,
        Guid? productVersionId,
        string? productVersionLabel,
        string? description,
        decimal quantity,
        decimal unitPrice,
        decimal discountPercent,
        decimal taxPercent,
        decimal lineTotal,
        bool isCustomized,
        bool requiresManufacturing,
        string? customizationJson,
        int sortOrder)
    {
        return new SalesOrderLine
        {
            Id = Guid.NewGuid(),
            SalesOrderId = salesOrderId,
            ProductId = productId,
            ProductSku = productSku,
            ProductName = productName,
            ProductVersionId = productVersionId,
            ProductVersionLabel = productVersionLabel,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            DiscountPercent = discountPercent,
            TaxPercent = taxPercent,
            LineTotal = lineTotal,
            QuantityDelivered = 0,
            QuantityInManufacturing = 0,
            IsCustomized = isCustomized,
            RequiresManufacturing = requiresManufacturing,
            CustomizationJson = customizationJson,
            SortOrder = sortOrder,
        };
    }

    public void ApplySnapshots(string? bomSnapshotJson, string? costSnapshotJson, string? customizationJson)
    {
        BomSnapshotJson = bomSnapshotJson;
        CostSnapshotJson = costSnapshotJson;
        if (customizationJson is not null) CustomizationJson = customizationJson;
    }

    public void RecordDelivery(decimal quantity)
    {
        if (quantity <= 0) return;
        QuantityDelivered = Math.Min(Quantity, QuantityDelivered + quantity);
    }

    public void SetQuantityDelivered(decimal quantity)
    {
        QuantityDelivered = Math.Clamp(quantity, 0, Quantity);
    }

    public void AllocateToManufacturing(decimal quantity)
    {
        if (quantity <= 0) return;
        QuantityInManufacturing = Math.Min(Quantity, QuantityInManufacturing + quantity);
    }
}
