using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

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
