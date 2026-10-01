namespace Delivery.Application.Deliveries;

public sealed record DeliveryItemInputDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    string Unit);
