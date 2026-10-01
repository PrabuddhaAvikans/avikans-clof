using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Inventory.Application.Abstractions;
using Inventory.Application.Warehouses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Inventory.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Inventory.Warehouses)]
[ApiController]
public sealed class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<WarehouseDto>>> List(
        [FromQuery] WarehouseListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<WarehouseDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetByIdAsync(id, cancellationToken);
        return warehouse is null ? NotFound() : Ok(warehouse);
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create(
        [FromBody] CreateWarehouseCommand command,
        CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
    }

    [HttpPut(ApiRoutes.ById)]
    public async Task<ActionResult<WarehouseDto>> Update(
        Guid id,
        [FromBody] UpdateWarehouseCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _warehouseService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete(ApiRoutes.ById)]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Warehouse deleted successfully."));
    }
}
