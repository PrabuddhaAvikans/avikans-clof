using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Inventory.Application.Items;

public sealed record CreateInventoryItemCommand(
    string Sku,
    string Name,
    string? Description,
    string Category,
    string ItemType,
    string Unit,
    string? Brand,
    string? Supplier,
    string? TaxCode,
    decimal QuantityOnHand,
    string Warehouse,
    string Location,
    decimal MinStock,
    decimal MaxStock,
    decimal ReorderLevel,
    decimal ReorderQuantity,
    decimal? BuyingPrice,
    decimal CostPrice,
    string PricingMethod,
    decimal MarkupPercent,
    decimal MarkupFixedAmount,
    decimal SellingPrice,
    DateTimeOffset PricingEffectiveDate,
    string Status);
