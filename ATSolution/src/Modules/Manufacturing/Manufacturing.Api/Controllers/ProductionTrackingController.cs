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

    [HttpGet("snapshot")]
    public async Task<ActionResult<ProductionTrackingSnapshotDto>> GetSnapshot(CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.GetSnapshotAsync(cancellationToken));
    }

    [HttpGet("jobs")]
    public async Task<ActionResult<PaginatedResponse<ProductionJobDto>>> ListJobs(
        [FromQuery] ProductionTrackingFilters query,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.ListJobsAsync(query, cancellationToken));
    }

    [HttpGet("jobs/{id:guid}")]
    public async Task<ActionResult<ProductionJobDto>> GetJobById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _productionTrackingService.GetJobByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost("start")]
    public async Task<ActionResult<ApiResponse>> StartProduction(
        [FromBody] StartProductionRequestDto request,
        CancellationToken cancellationToken)
    {
        await _productionTrackingService.StartProductionAsync(request.Ids ?? [], cancellationToken);
        return Ok(ApiResponse.Succeeded("Production started."));
    }

    [HttpPost("jobs/{id:guid}/update-stage")]
    public async Task<ActionResult<ProductionJobDto>> UpdateStage(
        Guid id,
        [FromBody] UpdateStageRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.UpdateStageAsync(id, request?.Comment, ResolveActor(), cancellationToken));
    }

    [HttpPost("jobs/{id:guid}/hold")]
    public async Task<ActionResult<ProductionJobDto>> Hold(
        Guid id,
        [FromBody] HoldProductionRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _productionTrackingService.HoldJobAsync(id, request?.Reason, ResolveActor(), cancellationToken));
    }

    [HttpPost("jobs/{id:guid}/release-to-qc")]
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
