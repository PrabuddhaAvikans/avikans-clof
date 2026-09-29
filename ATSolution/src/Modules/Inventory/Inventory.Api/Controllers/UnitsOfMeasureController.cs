using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Units;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Inventory.UnitsOfMeasure)]
[ApiController]
public sealed class UnitsOfMeasureController : ControllerBase
{
    private readonly IUnitOfMeasureService _unitOfMeasureService;

    public UnitsOfMeasureController(IUnitOfMeasureService unitOfMeasureService)
    {
        _unitOfMeasureService = unitOfMeasureService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<UnitOfMeasureDto>>> List(
        [FromQuery] UnitOfMeasureListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _unitOfMeasureService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var unit = await _unitOfMeasureService.GetByIdAsync(id, cancellationToken);
        return unit is null ? NotFound() : Ok(unit);
    }

    [HttpPost]
    public async Task<ActionResult<UnitOfMeasureDto>> Create(
        [FromBody] CreateUnitOfMeasureCommand command,
        CancellationToken cancellationToken)
    {
        var unit = await _unitOfMeasureService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = unit.Id }, unit);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UnitOfMeasureDto>> Update(
        Guid id,
        [FromBody] UpdateUnitOfMeasureCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _unitOfMeasureService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _unitOfMeasureService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Unit of measure deleted successfully."));
    }
}
