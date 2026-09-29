using ATSolution.SharedKernel.Models;
using Delivery.Application.Deliveries;

namespace Delivery.Application.Abstractions;

public interface IDeliveryService
{
    Task<PaginatedResponse<DeliveryDto>> ListAsync(
        DeliveryListQuery query,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryDto> CreateAsync(
        CreateDeliveryCommand command,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto> UpdateAsync(
        UpdateDeliveryCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryDto> UpdateStatusAsync(
        UpdateDeliveryStatusCommand command,
        CancellationToken cancellationToken = default);

    Task<DeliveryDto> DispatchAsync(Guid id, CancellationToken cancellationToken = default);

    Task<DeliveryDto> RecordProofOfDeliveryAsync(
        RecordProofOfDeliveryCommand command,
        CancellationToken cancellationToken = default);
}
