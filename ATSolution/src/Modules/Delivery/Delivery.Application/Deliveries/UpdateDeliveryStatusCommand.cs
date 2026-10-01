using ATSolution.SharedKernel.Models;

namespace Delivery.Application.Deliveries;

public sealed record UpdateDeliveryStatusCommand(Guid Id, string Status);
