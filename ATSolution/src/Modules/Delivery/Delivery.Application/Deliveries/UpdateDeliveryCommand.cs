using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

public sealed record UpdateDeliveryCommand(
    Guid Id,
    string? Priority,
    DateTimeOffset? ScheduledDate,
    IReadOnlyList<DeliveryItemInputDto>? Items,
    Guid? DriverId,
    string? VehicleNumber,
    string? Carrier,
    string? Notes);
