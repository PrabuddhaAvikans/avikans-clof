using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Finance.Api.DTOs.Requests;
using Finance.Application.Abstractions;
using Finance.Application.CreditNotes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Finance.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Finance.CreditNotes)]
[ApiController]
public sealed class CreditNotesController : ControllerBase
{
    private readonly ICreditNoteService _creditNoteService;

    public CreditNotesController(ICreditNoteService creditNoteService)
    {
        _creditNoteService = creditNoteService;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CreditNoteDto>>> List(
        [FromQuery] CreditNoteListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _creditNoteService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<CreditNoteDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var creditNote = await _creditNoteService.GetByIdAsync(id, cancellationToken);
        return creditNote is null ? NotFound() : Ok(creditNote);
    }

    [HttpPost]
    public async Task<ActionResult<CreditNoteDto>> Create(
        [FromBody] CreateCreditNoteCommand command,
        CancellationToken cancellationToken)
    {
        var creditNote = await _creditNoteService.CreateAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = creditNote.Id }, creditNote);
    }

    [HttpPut(ApiRoutes.ById)]
    public async Task<ActionResult<CreditNoteDto>> Update(
        Guid id,
        [FromBody] UpdateCreditNoteCommand request,
        CancellationToken cancellationToken)
    {
        return Ok(await _creditNoteService.UpdateAsync(request with { Id = id }, cancellationToken));
    }

    [HttpPost(ApiRoutes.Finance.Issue)]
    public async Task<ActionResult<CreditNoteDto>> Issue(
        Guid id,
        [FromBody] IssueCreditNoteRequest? request,
        CancellationToken cancellationToken)
    {
        return Ok(await _creditNoteService.IssueAsync(
            new IssueCreditNoteCommand(id, request?.IssuedBy, request?.IssuedByName),
            cancellationToken));
    }

    [HttpPost(ApiRoutes.Finance.Void)]
    public async Task<ActionResult<CreditNoteDto>> Void(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _creditNoteService.VoidAsync(new VoidCreditNoteCommand(id), cancellationToken));
    }

    [HttpPost(ApiRoutes.Finance.Apply)]
    public async Task<ActionResult<CreditNoteDto>> Apply(
        Guid id,
        [FromBody] ApplyCreditNoteRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(await _creditNoteService.ApplyToInvoiceAsync(
            new ApplyCreditNoteCommand(
                id,
                request.InvoiceId,
                request.Amount,
                request.Note,
                request.AppliedBy,
                request.AppliedByName),
            cancellationToken));
    }
}
