using System.Security.Claims;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Manufacturing.Api.DTOs.Requests;
using Manufacturing.Application.Abstractions;
using Manufacturing.Application.Jobs;
using Manufacturing.Application.ProductionTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Manufacturing.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Manufacturing.ProductionTracking)]
[ApiController]
public sealed class ProductionTrackingController : ControllerBase
{
    private readonly IProductionTrackingService _productionTrackingService;

    public ProductionTrackingController(IProductionTrackingService productionTrackingService)
    {
        _productionTrackingService = productionTrackingService;
    }

    [HttpGet(ApiRoutes.Manufacturing.Snapshot)]
    public async Task<ActionResult<ProductionTrackingSnapshotDto>> GetSnapshot(CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.GetSnapshotAsync(cancellationToken));
    }

    [HttpGet(ApiRoutes.Manufacturing.TrackingJobs)]
    public async Task<ActionResult<PaginatedResponse<ProductionJobDto>>> ListJobs(
        [FromQuery] ProductionTrackingFilters query,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.ListJobsAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.Manufacturing.TrackingJobById)]
    public async Task<ActionResult<ProductionJobDto>> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _productionTrackingService.GetJobByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost(ApiRoutes.Manufacturing.TrackingStart)]
    public async Task<ActionResult<ApiResponse>> StartProduction(
        [FromBody] StartProductionRequestDto request,
        CancellationToken cancellationToken)
    {
        await _productionTrackingService.StartProductionAsync(request.Ids ?? [], cancellationToken);
        return Ok(ApiResponse.Succeeded("Production started."));
    }

    [HttpPost(ApiRoutes.Manufacturing.UpdateStage)]
    public async Task<ActionResult<ProductionJobDto>> UpdateStage(
        Guid id,
        [FromBody] UpdateStageRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.UpdateStageAsync(id, request?.Comment, ResolveActor(), cancellationToken));
    }

    [HttpPost(ApiRoutes.Manufacturing.TrackingHold)]
    public async Task<ActionResult<ProductionJobDto>> Hold(
        Guid id,
        [FromBody] HoldProductionRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.HoldJobAsync(id, request?.Reason, ResolveActor(), cancellationToken));
    }

    [HttpPost(ApiRoutes.Manufacturing.ReleaseToQc)]
    public async Task<ActionResult<ProductionJobDto>> ReleaseToQc(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.ReleaseToQcAsync(id, ResolveActor(), cancellationToken));
    }

    private TaskActionActorDto ResolveActor()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "User";
        var userId = Guid.TryParse(idClaim, out var parsed) ? parsed : Guid.Empty;
        return new TaskActionActorDto(userId, name);
    }
}
