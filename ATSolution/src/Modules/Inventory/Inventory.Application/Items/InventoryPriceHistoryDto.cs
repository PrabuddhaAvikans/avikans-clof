using System.Text.Json;
namespace Inventory.Application.Items;

public sealed record InventoryPriceHistoryDto(
    Guid Id,
    Guid InventoryItemId,
    decimal? BuyingPrice,
    decimal CostPrice,
    decimal SellingPrice,
    string PricingMethod,
    decimal MarkupPercent,
    decimal MarkupFixedAmount,
    DateTimeOffset EffectiveDate,
    string ChangedBy,
    string ChangedByName,
    DateTimeOffset CreatedAt);
