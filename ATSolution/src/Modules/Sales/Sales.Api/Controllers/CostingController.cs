using ATSolution.SharedKernel.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.DTOs.Requests;
using Sales.Application.Abstractions;
using Sales.Application.Costing;
using Sales.Application.SalesOrders;

namespace Sales.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Sales.Costing)]
[ApiController]
public sealed class CostingController : ControllerBase
{
    private readonly ICostingService _costingService;

    public CostingController(ICostingService costingService)
    {
        _costingService = costingService;
    }

    [HttpGet]
    public async Task<ActionResult<ATSolution.SharedKernel.Models.PaginatedResponse<CostingRequestDto>>> List(
        [FromQuery] CostingListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<CostingRequestDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var item = await _costingService.GetByIdAsync(id, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }

    [HttpGet(ApiRoutes.Sales.BySalesOrder)]
    public async Task<ActionResult<CostingRequestDto?>> GetBySalesOrderId(
        Guid salesOrderId,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.GetBySalesOrderIdAsync(salesOrderId, cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.FromSalesOrderById)]
    public async Task<ActionResult<CostingRequestDto>> CreateFromSalesOrder(
        Guid salesOrderId,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.CreateFromSalesOrderAsync(salesOrderId, cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.SyncFromSalesOrderById)]
    public async Task<ActionResult<CostingRequestDto>> SyncFromSalesOrder(
        Guid salesOrderId,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.SyncFromSalesOrderAsync(salesOrderId, cancellationToken));
    }

    /// <summary>
    /// Accepts a full sales order payload from the client and syncs/creates costing for that order id.
    /// </summary>
    [HttpPost(ApiRoutes.Sales.FromSalesOrder)]
    public async Task<ActionResult<CostingRequestDto>> CreateFromSalesOrderBody(
        [FromBody] SalesOrderDto order,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.CreateFromSalesOrderAsync(order.Id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.SyncFromSalesOrder)]
    public async Task<ActionResult<CostingRequestDto>> SyncFromSalesOrderBody(
        [FromBody] SalesOrderDto order,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.SyncFromSalesOrderAsync(order.Id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.SubmitCoating)]
    public async Task<ActionResult<CostingRequestDto>> SubmitCoating(
        Guid id,
        [FromBody] SubmitCoatingRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.SubmitCoatingAsync(
            new SubmitCoatingCommand(id, request.Items, request.Materials, request.Notes, request.ActorName),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.Approve)]
    public async Task<ActionResult<CostingRequestDto>> Approve(
        Guid id,
        [FromBody] CommentRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.ApproveAsync(
            new CostingDecisionCommand(id, request?.Comment, request?.ActorName),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.Reject)]
    public async Task<ActionResult<CostingRequestDto>> Reject(
        Guid id,
        [FromBody] CommentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.RejectAsync(
            new CostingDecisionCommand(id, request.Comment ?? string.Empty, request.ActorName),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.RequestChanges)]
    public async Task<ActionResult<CostingRequestDto>> RequestChanges(
        Guid id,
        [FromBody] CommentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.RequestChangesAsync(
            new CostingDecisionCommand(id, request.Comment ?? string.Empty, request.ActorName),
            cancellationToken));
    }

    [HttpPut(ApiRoutes.Sales.Notes)]
    public async Task<ActionResult<CostingRequestDto>> UpdateNotes(
        Guid id,
        [FromBody] NotesBodyDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.UpdateNotesAsync(
            new UpdateCostingNotesCommand(id, request.Notes),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.Sales.Comments)]
    public async Task<ActionResult<CostingRequestDto>> AddComment(
        Guid id,
        [FromBody] CommentRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _costingService.AddCommentAsync(
            new CostingCommentCommand(id, request.Comment ?? string.Empty, request.ActorName),
            cancellationToken));
    }
}

