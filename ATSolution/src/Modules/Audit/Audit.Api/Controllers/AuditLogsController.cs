using ATSolution.SharedKernel.Constants;
using Audit.Application.Abstractions;
using Audit.Application.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Audit.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Audit.Base)]
[ApiController]
public sealed class AuditLogsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditLogsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult> List([FromQuery] AuditLogListQuery query, CancellationToken cancellationToken)
    {
        return Ok(await _auditService.ListAsync(query, cancellationToken));
    }

    [HttpGet(ApiRoutes.ById)]
    public async Task<ActionResult<AuditLogEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _auditService.GetByIdAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpGet(ApiRoutes.Audit.Summary)]
    public async Task<ActionResult<AuditLogSummaryDto>> Summary(
        [FromQuery] AuditLogListQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _auditService.SummaryAsync(query, cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<AuditLogEntryDto>> Append(
        [FromBody] AppendAuditLogCommand command,
        CancellationToken cancellationToken)
    {
        var entry = await _auditService.AppendAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = entry.Id }, entry);
    }
}
