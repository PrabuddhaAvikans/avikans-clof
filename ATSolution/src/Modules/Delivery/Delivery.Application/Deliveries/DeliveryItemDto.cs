namespace Delivery.Application.Deliveries;

public sealed record DeliveryItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    string Unit);
