using System.Security.Claims;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Manufacturing.Api.DTOs.Requests;
using Manufacturing.Application.Abstractions;
using Manufacturing.Application.Jobs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Manufacturing.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Manufacturing.Jobs)]
[ApiController]
public sealed class ManufacturingJobsController : ControllerBase
{
    private readonly IManufacturingService _manufacturingService;

    public ManufacturingJobsController(IManufacturingService manufacturingService)
    {
        _manufacturingService = manufacturingService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ManufacturingJobDto>>> List(
        [FromQuery] ManufacturingListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ManufacturingJobDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var job = await _manufacturingService.GetByIdAsync(id, cancellationToken);
        return job is null ? NotFound() : Ok(job);
    }

    [HttpPost]
    public async Task<ActionResult<ManufacturingJobDto>> Create(
        [FromBody] CreateManufacturingJobCommand command,
        CancellationToken cancellationToken)
    {
        var job = await _manufacturingService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = job.Id }, job);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ManufacturingJobDto>> Update(
        Guid id,
        [FromBody] UpdateManufacturingJobCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _manufacturingService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Manufacturing job deleted successfully."));
    }

    [HttpPost("{id:guid}/reserve-materials")]
    public async Task<ActionResult<ManufacturingJobDto>> ReserveMaterials(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.ReserveMaterialsAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ManufacturingJobDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.StartJobAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ManufacturingJobDto>> Complete(
        Guid id,
        [FromBody] ProductionCompletionInputDto? completion,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.CompleteJobAsync(id, completion, cancellationToken));
    }

    [HttpPost("{id:guid}/hold")]
    public async Task<ActionResult<ManufacturingJobDto>> Hold(
        Guid id,
        [FromBody] HoldJobRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.HoldJobAsync(id, request?.Reason, cancellationToken));
    }

    [HttpPost("{id:guid}/task-actions")]
    public async Task<ActionResult<ManufacturingJobDto>> ApplyTaskAction(
        Guid id,
        [FromBody] ManufacturingTaskActionDto action,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.ApplyTaskActionAsync(id, action, ResolveActor(), cancellationToken));
    }

    [HttpPost("{id:guid}/complete-tasks")]
    public async Task<ActionResult<ManufacturingJobDto>> CompleteTasks(
        Guid id,
        [FromBody] BulkCompleteTasksInputDto input,
        CancellationToken cancellationToken)
    {
        return Ok(await _manufacturingService.CompleteTasksAsync(id, input, ResolveActor(), cancellationToken));
    }

    private TaskActionActorDto ResolveActor()
    {
        var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = User.FindFirstValue(ClaimTypes.Name) ?? "User";
        var userId = Guid.TryParse(idClaim, out var parsed) ? parsed : Guid.Empty;
        return new TaskActionActorDto(userId, name);
    }
}
