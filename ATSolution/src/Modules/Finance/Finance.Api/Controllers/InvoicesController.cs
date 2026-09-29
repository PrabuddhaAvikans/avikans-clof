using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Finance.Api.DTOs.Requests;
using Finance.Application.Abstractions;
using Finance.Application.Invoices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Finance.Invoices)]
[ApiController]
public sealed class InvoicesController : ControllerBase
{
    private readonly IInvoiceService _invoiceService;

    public InvoicesController(IInvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<InvoiceDto>>> List(
        [FromQuery] InvoiceListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _invoiceService.ListAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.GetByIdAsync(id, cancellationToken);
        return invoice is null ? NotFound() : Ok(invoice);
    }

    [HttpPost]
    public async Task<ActionResult<InvoiceDto>> Create(
        [FromBody] CreateInvoiceCommand command,
        CancellationToken cancellationToken)
    {
        var invoice = await _invoiceService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = invoice.Id }, invoice);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<InvoiceDto>> Update(
        Guid id,
        [FromBody] UpdateInvoiceCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _invoiceService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpPost("{id:guid}/issue")]
    public async Task<ActionResult<InvoiceDto>> Issue(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _invoiceService.IssueAsync(new IssueInvoiceCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/void")]
    public async Task<ActionResult<InvoiceDto>> Void(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _invoiceService.VoidAsync(new VoidInvoiceCommand(id), cancellationToken));
    }

    [HttpPost("{id:guid}/payments")]
    public async Task<ActionResult<InvoiceDto>> RecordPayment(
        Guid id,
        [FromBody] RecordPaymentRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _invoiceService.RecordPaymentAsync(
            new RecordInvoicePaymentCommand(id, request.Amount),
            cancellationToken));
    }
}
