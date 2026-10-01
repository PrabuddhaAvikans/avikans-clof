using System.Text.Json;
namespace Sales.Application.SalesOrders;

public sealed record SalesOrderLineDto(
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
    decimal QuantityDelivered,
    decimal QuantityInManufacturing,
    bool IsCustomized,
    JsonElement? Customization,
    bool RequiresManufacturing);
