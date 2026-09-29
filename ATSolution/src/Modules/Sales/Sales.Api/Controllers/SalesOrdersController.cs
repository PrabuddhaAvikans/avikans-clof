using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.DTOs.Requests;
using Sales.Application.Abstractions;
using Sales.Application.SalesOrders;

namespace Sales.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Sales.SalesOrders)]
[ApiController]
public sealed class SalesOrdersController : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService;

    public SalesOrdersController(ISalesOrderService salesOrderService)
    {
        _salesOrderService = salesOrderService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<SalesOrderDto>>> List(
        [FromQuery] SalesOrderListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _salesOrderService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.GetByIdAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<SalesOrderDto>> Create(
        [FromBody] CreateSalesOrderCommand command,
        CancellationToken cancellationToken)
    {
        var order = await _salesOrderService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<SalesOrderDto>> Update(
        Guid id,
        [FromBody] UpdateSalesOrderCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _salesOrderService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _salesOrderService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Sales order deleted successfully."));
    }

    [HttpPost("{id:guid}/confirm")]
    public async Task<ActionResult<SalesOrderDto>> Confirm(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _salesOrderService.ConfirmAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<SalesOrderDto>> Cancel(
        Guid id,
        [FromBody] CancelRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _salesOrderService.CancelAsync(
            new CancelSalesOrderCommand(id, request?.Reason),
            cancellationToken));
    }

    [HttpPost("{id:guid}/assign")]
    public async Task<ActionResult<SalesOrderDto>> Assign(
        Guid id,
        [FromBody] AssignRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _salesOrderService.AssignAsync(
            new AssignSalesOrderCommand(id, request.UserId),
            cancellationToken));
    }
}
