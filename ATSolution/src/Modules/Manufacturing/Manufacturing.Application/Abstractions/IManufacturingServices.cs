using ATSolution.SharedKernel.Models;
using Manufacturing.Application.Jobs;
using Manufacturing.Application.ProductionTracking;

namespace Manufacturing.Application.Abstractions;

public interface IManufacturingService
{
    Task<PaginatedResponse<ManufacturingJobDto>> ListAsync(
        ManufacturingListQuery query,
        CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> CreateAsync(
        CreateManufacturingJobCommand command,
        CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> UpdateAsync(
        UpdateManufacturingJobCommand command,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> ReserveMaterialsAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> StartJobAsync(Guid id, CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> CompleteJobAsync(
        Guid id,
        ProductionCompletionInputDto? completion,
        CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> HoldJobAsync(
        Guid id,
        string? reason,
        CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> ApplyTaskActionAsync(
        Guid jobId,
        ManufacturingTaskActionDto action,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default);

    Task<ManufacturingJobDto> CompleteTasksAsync(
        Guid jobId,
        BulkCompleteTasksInputDto input,
        TaskActionActorDto actor,
        CancellationToken cancellationToken = default);
}

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
