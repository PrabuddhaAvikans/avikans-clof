using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed record UpdateInventoryItemCommand(
    Guid Id,
    string? Sku = null,
    string? Name = null,
    string? Description = null,
    string? Category = null,
    string? ItemType = null,
    string? Unit = null,
    string? Brand = null,
    string? Supplier = null,
    string? TaxCode = null,
    decimal? QuantityOnHand = null,
    string? Warehouse = null,
    string? Location = null,
    decimal? MinStock = null,
    decimal? MaxStock = null,
    decimal? ReorderLevel = null,
    decimal? ReorderQuantity = null,
    decimal? BuyingPrice = null,
    decimal? CostPrice = null,
    string? PricingMethod = null,
    decimal? MarkupPercent = null,
    decimal? MarkupFixedAmount = null,
    decimal? SellingPrice = null,
    DateTimeOffset? PricingEffectiveDate = null,
    string? Status = null);
