using System.Text.Json;

namespace Inventory.Api.DTOs.Requests;

public sealed record UpdateInventoryItemRequestDto
{
    public string? Sku { get; init; }
    public string? Name { get; init; }
    public string? Description { get; init; }
    public string? Category { get; init; }
    public string? ItemType { get; init; }
    public string? Unit { get; init; }
    public string? Brand { get; init; }
    public string? Supplier { get; init; }
    public string? TaxCode { get; init; }
    public decimal? QuantityOnHand { get; init; }
    public string? Warehouse { get; init; }
    public string? Location { get; init; }
    public decimal? MinStock { get; init; }
    public decimal? MaxStock { get; init; }
    public decimal? ReorderLevel { get; init; }
    public decimal? ReorderQuantity { get; init; }
    public decimal? BuyingPrice { get; init; }
    public decimal? CostPrice { get; init; }
    public string? PricingMethod { get; init; }
    public decimal? MarkupPercent { get; init; }
    public decimal? MarkupFixedAmount { get; init; }
    public decimal? SellingPrice { get; init; }
    public DateTimeOffset? PricingEffectiveDate { get; init; }
    public string? Status { get; init; }
}
