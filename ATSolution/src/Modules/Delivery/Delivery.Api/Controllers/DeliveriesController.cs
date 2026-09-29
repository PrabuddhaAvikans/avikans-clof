using ATSolution.SharedKernel.Constants;

using ATSolution.SharedKernel.Models;

using Delivery.Api.DTOs.Requests;

using Delivery.Application.Abstractions;

using Delivery.Application.Deliveries;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;



namespace Delivery.Api.Controllers;



[Authorize]

[Route(ApiRoutes.Delivery.Base)]

[ApiController]

public sealed class DeliveriesController : ControllerBase

{

    private readonly IDeliveryService _deliveryService;



    public DeliveriesController(IDeliveryService deliveryService)

    {

        _deliveryService = deliveryService;

    }



    [HttpGet]

    public async Task<ActionResult<PaginatedResponse<DeliveryDto>>> List(

        [FromQuery] DeliveryListQuery query,

        CancellationToken cancellationToken)

    {

        return Ok(await _deliveryService.ListAsync(query, cancellationToken));

    }



    [HttpGet("{id:guid}")]

    public async Task<ActionResult<DeliveryDto>> GetById(Guid id, CancellationToken cancellationToken)

    {

        var delivery = await _deliveryService.GetByIdAsync(id, cancellationToken);

        return delivery is null ? NotFound() : Ok(delivery);

    }



    [HttpPost]

    public async Task<ActionResult<DeliveryDto>> Create(

        [FromBody] CreateDeliveryCommand command,

        CancellationToken cancellationToken)

    {

        var delivery = await _deliveryService.CreateAsync(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id = delivery.Id }, delivery);

    }



    [HttpPut("{id:guid}")]

    public async Task<ActionResult<DeliveryDto>> Update(

        Guid id,

        [FromBody] UpdateDeliveryCommand request,

        CancellationToken cancellationToken)

    {

        return Ok(await _deliveryService.UpdateAsync(request with { Id = id }, cancellationToken));

    }



    [HttpDelete("{id:guid}")]

    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)

    {

        await _deliveryService.DeleteAsync(id, cancellationToken);

        return Ok(ApiResponse.Succeeded("Delivery deleted successfully."));

    }



    [HttpPost("{id:guid}/status")]

    public async Task<ActionResult<DeliveryDto>> UpdateStatus(

        Guid id,

        [FromBody] UpdateDeliveryStatusRequestDto request,

        CancellationToken cancellationToken)

    {

        return Ok(await _deliveryService.UpdateStatusAsync(

            new UpdateDeliveryStatusCommand(id, request.Status),

            cancellationToken));

    }



    [HttpPost("{id:guid}/dispatch")]

    public async Task<ActionResult<DeliveryDto>> Dispatch(Guid id, CancellationToken cancellationToken)

    {

        return Ok(await _deliveryService.DispatchAsync(id, cancellationToken));

    }



    [HttpPost("{id:guid}/proof-of-delivery")]

    public async Task<ActionResult<DeliveryDto>> RecordProofOfDelivery(

        Guid id,

        [FromBody] RecordProofOfDeliveryRequestDto request,

        CancellationToken cancellationToken)

    {

        return Ok(await _deliveryService.RecordProofOfDeliveryAsync(

            new RecordProofOfDeliveryCommand(

                id,

                request.SignedBy,

                request.SignedAt,

                request.SignatureUrl,

                request.PhotoUrls,

                request.Notes,

                request.GpsCoordinates),

            cancellationToken));

    }

}

