using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Sales.Api.DTOs.Requests;
using Sales.Application.Abstractions;
using Sales.Application.Quotations;
using Sales.Application.SalesOrders;

namespace Sales.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Sales.Quotations)]
[ApiController]
public sealed class QuotationsController : ControllerBase
{
    private readonly IQuotationService _quotationService;

    public QuotationsController(IQuotationService quotationService)
    {
        _quotationService = quotationService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<QuotationDto>>> List(
        [FromQuery] QuotationListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var quotation = await _quotationService.GetByIdAsync(id, cancellationToken);
        return quotation is null ? NotFound() : Ok(quotation);
    }

    [HttpPost]
    public async Task<ActionResult<QuotationDto>> Create(
        [FromBody] CreateQuotationCommand command,
        CancellationToken cancellationToken)
    {
        var quotation = await _quotationService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = quotation.Id }, quotation);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<QuotationDto>> Update(
        Guid id,
        [FromBody] UpdateQuotationCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse>> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _quotationService.DeleteAsync(id, cancellationToken);
        return Ok(ApiResponse.Succeeded("Quotation deleted successfully."));
    }

    [HttpPost("{id:guid}/send")]
    public async Task<ActionResult<QuotationDto>> Send(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.SendAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/convert-to-sales-order")]
    public async Task<ActionResult<SalesOrderDto>> ConvertToSalesOrder(
        Guid id,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.ConvertToSalesOrderAsync(id, cancellationToken));
    }

    [HttpPost("{id:guid}/contacts")]
    public async Task<ActionResult<QuotationDto>> AddContactEntry(
        Guid id,
        [FromBody] AddQuotationContactRequestDto request,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.AddContactEntryAsync(
            new AddQuotationContactCommand(
                id,
                request.Type,
                request.Summary,
                request.Detail,
                request.Outcome,
                request.ContactedBy,
                request.ContactedByName),
            cancellationToken));
    }

    [HttpPost("{quotationId:guid}/lines/{lineItemId:guid}/approve-customization")]
    public async Task<ActionResult<QuotationDto>> ApproveLineCustomization(
        Guid quotationId,
        Guid lineItemId,
        [FromBody] NotesRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.ApproveLineCustomizationAsync(
            quotationId,
            lineItemId,
            request?.Notes,
            cancellationToken));
    }

    [HttpPost("{quotationId:guid}/lines/{lineItemId:guid}/promote-customization")]
    public async Task<ActionResult<PromoteCustomizationResultDto>> PromoteCustomization(
        Guid quotationId,
        Guid lineItemId,
        [FromBody] PromoteCustomizationRequestDto? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _quotationService.PromoteCustomizationToProductVersionAsync(
            quotationId,
            lineItemId,
            request?.RevisionNotes,
            cancellationToken));
    }
}
