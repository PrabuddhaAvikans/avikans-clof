using ATSolution.SharedKernel.Models;
using Manufacturing.Application.Jobs;
using Manufacturing.Application.ProductionTracking;

namespace Manufacturing.Application.Abstractions;

public interface IProductionTrackingService
{
    Task<ProductionTrackingSnapshotDto> GetSnapshotAsync(CancellationToken cancellationToken = default);

    Task<PaginatedResponse<ProductionJobDto>> ListJobsAsync(
        ProductionTrackingFilters query,
        CancellationToken cancellationToken = default);

    Task<ProductionJobDto?> GetJobByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task StartProductionAsync(IReadOnlyList<Guid> ids, CancellationToken cancellationToken = default);

    Task<ProductionJobDto> UpdateStageAsync(
        Guid id,
        string? comment,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default);

    Task<ProductionJobDto> HoldJobAsync(
        Guid id,
        string? reason,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default);

    Task<ProductionJobDto> ReleaseToQcAsync(
        Guid id,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default);
}
