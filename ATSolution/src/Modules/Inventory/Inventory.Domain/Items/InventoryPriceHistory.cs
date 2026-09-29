using ATSolution.Domain.Entities.Common;

namespace Inventory.Domain.Items;

public class InventoryPriceHistory : Entity<Guid>
{
    public Guid InventoryItemId { get; private set; }
    public InventoryItem InventoryItem { get; private set; } = null!;
    public decimal? BuyingPrice { get; private set; }
    public decimal CostPrice { get; private set; }
    public decimal SellingPrice { get; private set; }
    public string PricingMethod { get; private set; } = null!;
    public decimal MarkupPercent { get; private set; }
    public decimal MarkupFixedAmount { get; private set; }
    public DateTimeOffset EffectiveDateUtc { get; private set; }
    public string ChangedBy { get; private set; } = null!;
    public string ChangedByName { get; private set; } = null!;
    public DateTimeOffset CreatedOnUtc { get; private set; }

    public static InventoryPriceHistory Create(
        Guid inventoryItemId,
        decimal? buyingPrice,
        decimal costPrice,
        decimal sellingPrice,
        string pricingMethod,
        decimal markupPercent,
        decimal markupFixedAmount,
        DateTimeOffset effectiveDateUtc,
        string changedBy,
        string changedByName)
    {
        return new InventoryPriceHistory
        {
            Id = Guid.NewGuid(),
            InventoryItemId = inventoryItemId,
            BuyingPrice = buyingPrice,
            CostPrice = costPrice,
            SellingPrice = sellingPrice,
            PricingMethod = pricingMethod,
            MarkupPercent = markupPercent,
            MarkupFixedAmount = markupFixedAmount,
            EffectiveDateUtc = effectiveDateUtc,
            ChangedBy = changedBy,
            ChangedByName = changedByName,
            CreatedOnUtc = DateTimeOffset.UtcNow,
        };
    }
}
