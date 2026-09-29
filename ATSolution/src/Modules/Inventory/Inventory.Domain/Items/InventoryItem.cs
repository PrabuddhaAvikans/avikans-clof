using ATSolution.Domain.Entities.Common;
using Inventory.Domain.Common;

namespace Inventory.Domain.Items;

public class InventoryItem : Entity<Guid>, IAuditableEntity
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Category { get; private set; } = null!;
    public string ItemType { get; private set; } = null!;
    public string Unit { get; private set; } = null!;
    public string? Brand { get; private set; }
    public string? Supplier { get; private set; }
    public string? TaxCode { get; private set; }
    public decimal QuantityOnHand { get; private set; }
    public decimal QuantityReserved { get; private set; }
    public string Warehouse { get; private set; } = null!;
    public string Location { get; private set; } = null!;
    public decimal MinStock { get; private set; }
    public decimal MaxStock { get; private set; }
    public decimal ReorderLevel { get; private set; }
    public decimal ReorderQuantity { get; private set; }
    public decimal? BuyingPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public string PricingMethod { get; private set; } = "manual";
    public decimal MarkupPercent { get; private set; }
    public decimal MarkupFixedAmount { get; private set; }
    public decimal SellingPrice { get; private set; }
    public DateTimeOffset PricingEffectiveDate { get; private set; }
    public string StockStatus { get; private set; } = StockStatuses.OutOfStock;
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset? LastRestockedAtUtc { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public decimal QuantityAvailable => QuantityOnHand - QuantityReserved;

    public static InventoryItem Create(
        string sku,
        string name,
        string? description,
        string category,
        string itemType,
        string unit,
        string? brand,
        string? supplier,
        string? taxCode,
        decimal quantityOnHand,
        string warehouse,
        string location,
        decimal minStock,
        decimal maxStock,
        decimal reorderLevel,
        decimal reorderQuantity,
        decimal? buyingPrice,
        decimal costPrice,
        string pricingMethod,
        decimal markupPercent,
        decimal markupFixedAmount,
        decimal sellingPrice,
        DateTimeOffset pricingEffectiveDate,
        string status)
    {
        var now = DateTimeOffset.UtcNow;
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            Sku = sku.Trim(),
            Name = name.Trim(),
            Description = description,
            Category = category,
            ItemType = itemType,
            Unit = unit,
            Brand = brand,
            Supplier = supplier,
            TaxCode = taxCode,
            QuantityOnHand = quantityOnHand,
            QuantityReserved = 0,
            Warehouse = warehouse,
            Location = location,
            MinStock = minStock,
            MaxStock = maxStock,
            ReorderLevel = reorderLevel,
            ReorderQuantity = reorderQuantity,
            BuyingPrice = buyingPrice,
            CostPrice = costPrice,
            PricingMethod = pricingMethod,
            MarkupPercent = markupPercent,
            MarkupFixedAmount = markupFixedAmount,
            SellingPrice = sellingPrice,
            PricingEffectiveDate = pricingEffectiveDate,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
        item.RefreshStockStatus();
        return item;
    }

    public void UpdateDetails(
        string? name,
        string? description,
        string? category,
        string? itemType,
        string? unit,
        string? brand,
        string? supplier,
        string? taxCode,
        string? warehouse,
        string? location,
        decimal? minStock,
        decimal? maxStock,
        decimal? reorderLevel,
        decimal? reorderQuantity,
        decimal? buyingPrice,
        decimal? costPrice,
        string? pricingMethod,
        decimal? markupPercent,
        decimal? markupFixedAmount,
        decimal? sellingPrice,
        DateTimeOffset? pricingEffectiveDate,
        string? status,
        decimal? quantityOnHand)
    {
        if (name is not null) Name = name.Trim();
        if (description is not null) Description = description;
        if (category is not null) Category = category;
        if (itemType is not null) ItemType = itemType;
        if (unit is not null) Unit = unit;
        if (brand is not null) Brand = brand;
        if (supplier is not null) Supplier = supplier;
        if (taxCode is not null) TaxCode = taxCode;
        if (warehouse is not null) Warehouse = warehouse;
        if (location is not null) Location = location;
        if (minStock.HasValue) MinStock = minStock.Value;
        if (maxStock.HasValue) MaxStock = maxStock.Value;
        if (reorderLevel.HasValue) ReorderLevel = reorderLevel.Value;
        if (reorderQuantity.HasValue) ReorderQuantity = reorderQuantity.Value;
        if (buyingPrice.HasValue) BuyingPrice = buyingPrice;
        if (costPrice.HasValue) CostPrice = costPrice.Value;
        if (pricingMethod is not null) PricingMethod = pricingMethod;
        if (markupPercent.HasValue) MarkupPercent = markupPercent.Value;
        if (markupFixedAmount.HasValue) MarkupFixedAmount = markupFixedAmount.Value;
        if (sellingPrice.HasValue) SellingPrice = sellingPrice.Value;
        if (pricingEffectiveDate.HasValue) PricingEffectiveDate = pricingEffectiveDate.Value;
        if (status is not null) Status = status;
        if (quantityOnHand.HasValue) QuantityOnHand = quantityOnHand.Value;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        RefreshStockStatus();
    }

    public void ApplyReceipt(decimal quantity)
    {
        QuantityOnHand += Math.Abs(quantity);
        LastRestockedAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = LastRestockedAtUtc.Value;
        RefreshStockStatus();
    }

    public void ApplyIssue(decimal quantity)
    {
        var abs = Math.Abs(quantity);
        EnsureAvailable(abs);
        QuantityOnHand -= abs;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        RefreshStockStatus();
    }

    public void ApplyReservation(decimal quantity)
    {
        var abs = Math.Abs(quantity);
        EnsureAvailable(abs);
        QuantityReserved += abs;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        RefreshStockStatus();
    }

    public void ApplyRelease(decimal quantity)
    {
        QuantityReserved = Math.Max(0, QuantityReserved - Math.Abs(quantity));
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        RefreshStockStatus();
    }

    public void ApplyAdjustment(decimal delta)
    {
        QuantityOnHand += delta;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        RefreshStockStatus();
    }

    public void ApplyTransfer(decimal quantity)
    {
        ApplyIssue(quantity);
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private void EnsureAvailable(decimal quantity)
    {
        if (quantity > QuantityAvailable + 0.000000001m)
        {
            throw new InvalidOperationException(
                $"Insufficient available stock for {Sku}. Available: {QuantityAvailable}, requested: {quantity}.");
        }
    }

    private void RefreshStockStatus()
    {
        if (QuantityAvailable <= 0)
        {
            StockStatus = StockStatuses.OutOfStock;
        }
        else if (QuantityAvailable <= ReorderLevel)
        {
            StockStatus = StockStatuses.LowStock;
        }
        else if (QuantityReserved > 0)
        {
            StockStatus = StockStatuses.Reserved;
        }
        else
        {
            StockStatus = StockStatuses.InStock;
        }
    }
}
