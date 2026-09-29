using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

public sealed class DeliveryListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? SalesOrderId { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Priority { get; set; }
    public Guid? DriverId { get; set; }
}

public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record DeliveryItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    string Unit);

public sealed record ProofOfDeliveryDto(
    Guid Id,
    string SignedBy,
    DateTimeOffset SignedAt,
    string? SignatureUrl,
    IReadOnlyList<string> PhotoUrls,
    string? Notes,
    GpsCoordinatesDto? GpsCoordinates);

public sealed record GpsCoordinatesDto(decimal Lat, decimal Lng);

public sealed record DeliveryDto(
    Guid Id,
    string DeliveryNumber,
    Guid SalesOrderId,
    string SalesOrderNumber,
    Guid CustomerId,
    string CustomerName,
    string Status,
    string Priority,
    IReadOnlyList<DeliveryItemDto> Items,
    AddressDto ShippingAddress,
    string? Carrier,
    string? TrackingNumber,
    Guid? DriverId,
    string? DriverName,
    string? VehicleNumber,
    DateTimeOffset ScheduledDate,
    DateTimeOffset? DispatchedAt,
    DateTimeOffset? DeliveredAt,
    ProofOfDeliveryDto? ProofOfDelivery,
    string? Notes,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DeliveryItemInputDto(
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal QuantityOrdered,
    decimal QuantityDelivered,
    string Unit);

public sealed record CreateDeliveryCommand(
    Guid SalesOrderId,
    IReadOnlyList<DeliveryItemInputDto> Items,
    DateTimeOffset ScheduledDate,
    string Priority,
    string? Carrier,
    Guid? DriverId,
    string? VehicleNumber,
    string? Notes,
    string? CreatedBy,
    string? CreatedByName);

public sealed record UpdateDeliveryCommand(
    Guid Id,
    string? Priority,
    DateTimeOffset? ScheduledDate,
    IReadOnlyList<DeliveryItemInputDto>? Items,
    Guid? DriverId,
    string? VehicleNumber,
    string? Carrier,
    string? Notes);

public sealed record UpdateDeliveryStatusCommand(Guid Id, string Status);

public sealed record RecordProofOfDeliveryCommand(
    Guid Id,
    string SignedBy,
    DateTimeOffset SignedAt,
    string? SignatureUrl,
    IReadOnlyList<string>? PhotoUrls,
    string? Notes,
    GpsCoordinatesDto? GpsCoordinates);
