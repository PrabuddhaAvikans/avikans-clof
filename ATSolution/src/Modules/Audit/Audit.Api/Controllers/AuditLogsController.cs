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

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AuditLogEntryDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var entry = await _auditService.GetByIdAsync(id, cancellationToken);
        return entry is null ? NotFound() : Ok(entry);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<AuditLogSummaryDto>> Summary(
        [FromQuery] AuditLogSummaryQuery query,
        CancellationToken cancellationToken)
    {
        return Ok(await _auditService.SummaryAsync(query, cancellationToken));
    }
}
