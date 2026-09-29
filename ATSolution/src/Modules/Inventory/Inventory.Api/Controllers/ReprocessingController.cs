using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Api.DTOs.Requests;
using Inventory.Application.Abstractions;
using Inventory.Application.Reprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Inventory.Reprocessing)]
[ApiController]
public sealed class ReprocessingController : ControllerBase
{
    private readonly IReprocessingService _reprocessingService;

    public ReprocessingController(IReprocessingService reprocessingService)
    {
        _reprocessingService = reprocessingService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<ReprocessingBatchDto>>> List(
        [FromQuery] ReprocessingListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _reprocessingService.ListAsync(query, cancellationToken));
    }

    [HttpGet("scrap-lots")]
    public async Task<ActionResult<IReadOnlyList<ReusableScrapLotDto>>> ListReusableScrapLots(
        CancellationToken cancellationToken)
    {
        return Ok(await _reprocessingService.ListReusableScrapLotsAsync(cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ReprocessingBatchDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var batch = await _reprocessingService.GetByIdAsync(id, cancellationToken);
        return batch is null ? NotFound() : Ok(batch);
    }

    [HttpPost]
    public async Task<ActionResult<ReprocessingBatchDto>> Create(
        [FromBody] CreateReprocessingBatchCommand command,
        CancellationToken cancellationToken)
    {
        var batch = await _reprocessingService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = batch.Id }, batch);
    }

    [HttpPost("{id:guid}/start")]
    public async Task<ActionResult<ReprocessingBatchDto>> Start(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _reprocessingService.StartAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/complete")]
    public async Task<ActionResult<ReprocessingBatchDto>> Complete(
        Guid id,
        [FromBody] CompleteReprocessingRequestDto request,
        CancellationToken cancellationToken)
    {
        var batch = await _reprocessingService.CompleteAsync(
            new CompleteReprocessingCommand(
                id,
                request.RecoveredQuantity,
                request.ProcessLossQuantity,
                request.Costs,
                request.Notes),
            cancellationToken);
        return Ok(batch);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<ReprocessingBatchDto>> Cancel(
        Guid id,
        [FromBody] CancelReprocessingRequestDto? request,
        CancellationToken cancellationToken)
    {
        var batch = await _reprocessingService.CancelAsync(
            new CancelReprocessingCommand(id, request?.Reason),
            cancellationToken);
        return Ok(batch);
    }
}
