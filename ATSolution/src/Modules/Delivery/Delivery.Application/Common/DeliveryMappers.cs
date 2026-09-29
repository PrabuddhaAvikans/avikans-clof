using Delivery.Application.Deliveries;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;

namespace Delivery.Application.Common;

public static class DeliveryMappers
{
    public static AddressDto MapAddress(string? json)
    {
        var fallback = new AddressDto("", null, "", "", "", "");
        return JsonColumn.Deserialize(json, fallback);
    }

    public static IReadOnlyList<DeliveryItemDto> MapItems(string? json) =>
        JsonColumn.Deserialize(json, Array.Empty<DeliveryItemDto>());

    public static ProofOfDeliveryDto? MapProof(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        return JsonColumn.Deserialize<ProofOfDeliveryDto?>(json, null);
    }

    public static DeliveryDto MapDelivery(DeliveryEntity entity) =>
        new(
            entity.Id,
            entity.Number,
            entity.SalesOrderId,
            entity.SalesOrderNumber,
            entity.CustomerId,
            entity.CustomerName,
            entity.Status,
            entity.Priority,
            MapItems(entity.LineItemsJson),
            MapAddress(entity.ShippingAddressJson),
            entity.Carrier,
            entity.TrackingNumber,
            entity.DriverUserId,
            entity.DriverName,
            entity.Vehicle,
            entity.ScheduledDate,
            entity.DispatchedAtUtc,
            entity.DeliveredAtUtc,
            MapProof(entity.ProofOfDeliveryJson),
            entity.Notes,
            entity.CreatedBy,
            entity.CreatedByName,
            entity.CreatedOnUtc,
            entity.ModifiedOnUtc);

    public static string SerializeItems(IEnumerable<DeliveryItemInputDto> items) =>
        JsonColumn.Serialize(items.Select(item => new DeliveryItemDto(
            Guid.NewGuid(),
            item.ProductId,
            item.ProductSku,
            item.ProductName,
            item.QuantityOrdered,
            item.QuantityDelivered,
            item.Unit)).ToList());

    public static string SerializeItemsWithIds(IEnumerable<DeliveryItemDto> items) =>
        JsonColumn.Serialize(items.ToList());
}
