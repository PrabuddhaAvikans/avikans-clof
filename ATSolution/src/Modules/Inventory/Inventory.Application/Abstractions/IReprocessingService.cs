using ATSolution.SharedKernel.Models;
using Inventory.Application.Reprocessing;

namespace Inventory.Application.Abstractions;

public interface IReprocessingService
{
    Task<PaginatedResponse<ReprocessingBatchDto>> ListAsync(
        ReprocessingListQuery query,
        CancellationToken cancellationToken = default);

    Task<ReprocessingBatchDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReprocessingBatchDto> CreateAsync(
        CreateReprocessingBatchCommand command,
        CancellationToken cancellationToken = default);

    Task<ReprocessingBatchDto> StartAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ReprocessingBatchDto> CompleteAsync(
        CompleteReprocessingCommand command,
        CancellationToken cancellationToken = default);

    Task<ReprocessingBatchDto> CancelAsync(
        CancelReprocessingCommand command,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReusableScrapLotDto>> ListReusableScrapLotsAsync(
        CancellationToken cancellationToken = default);
}
