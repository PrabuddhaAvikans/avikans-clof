using ATSolution.SharedKernel.Constants;
using Configuration.Application.Abstractions;
using Configuration.Application.Workflows;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Configuration.Api.Controllers;

[Authorize]
[Route(ApiRoutes.Configuration.Workflows)]
[ApiController]
public sealed class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;

    public WorkflowsController(IWorkflowService workflowService)
    {
        _workflowService = workflowService;
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<WorkflowCatalogDto>> GetCatalog(CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.GetCatalogAsync(cancellationToken));
    }

    [HttpPost("definitions")]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpsertDefinition(
        [FromBody] UpsertWorkflowDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.UpsertDefinitionAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost("versions/{id:guid}/publish")]
    public async Task<ActionResult<WorkflowVersionDto>> PublishVersion(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.PublishVersionAsync(id, cancellationToken));
    }

    [HttpGet("instances")]
    public async Task<ActionResult<IReadOnlyList<WorkflowInstanceDto>>> ListInstances(CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.ListInstancesAsync(cancellationToken));
    }

    [HttpGet("instances/{id:guid}")]
    public async Task<ActionResult<WorkflowInstanceDto>> GetInstance(Guid id, CancellationToken cancellationToken)
    {
        var instance = await _workflowService.GetInstanceAsync(id, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }
}
