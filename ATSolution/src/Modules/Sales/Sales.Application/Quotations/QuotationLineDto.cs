using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record QuotationLineDto(
    Guid Id,
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    Guid? ProductVersionId,
    string? ProductVersionLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    bool IsCustomized,
    JsonElement? Customization,
    bool RequiresManufacturing);
