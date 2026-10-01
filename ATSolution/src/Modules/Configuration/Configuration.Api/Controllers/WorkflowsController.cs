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

    [HttpGet(ApiRoutes.Configuration.Catalog)]
    public async Task<ActionResult<WorkflowCatalogDto>> GetCatalog(CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.GetCatalogAsync(cancellationToken));
    }

    [HttpPost(ApiRoutes.Configuration.Definitions)]
    public async Task<ActionResult<WorkflowDefinitionDto>> UpsertDefinition(
        [FromBody] UpsertWorkflowDefinitionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _workflowService.UpsertDefinitionAsync(command, cancellationToken);
        return Ok(result);
    }

    [HttpPost(ApiRoutes.Configuration.VersionDraft)]
    public async Task<ActionResult<WorkflowCatalogDto>> CreateDraft(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.CreateDraftFromVersionAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Configuration.ApplyVersion)]
    public async Task<ActionResult<WorkflowCatalogDto>> ApplyDraft(
        Guid id,
        [FromBody] ApplyWorkflowDraftCommand command,
        CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.ApplyDraftAsync(id, command, cancellationToken));
    }

    [HttpPost(ApiRoutes.Configuration.PublishVersion)]
    public async Task<ActionResult<WorkflowVersionDto>> PublishVersion(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.PublishVersionAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Configuration.ActivateVersion)]
    public async Task<ActionResult<WorkflowCatalogDto>> ActivateVersion(Guid id, CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.ActivateVersionAsync(id, cancellationToken));
    }

    [HttpPost(ApiRoutes.Configuration.ResetCatalog)]
    public async Task<ActionResult<WorkflowCatalogDto>> ResetCatalog(CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.ResetCatalogAsync(cancellationToken));
    }

    [HttpGet(ApiRoutes.Configuration.Instances)]
    public async Task<ActionResult<IReadOnlyList<WorkflowInstanceDto>>> ListInstances(CancellationToken cancellationToken)
    {
        return Ok(await _workflowService.ListInstancesAsync(cancellationToken));
    }

    [HttpGet(ApiRoutes.Configuration.InstanceById)]
    public async Task<ActionResult<WorkflowInstanceDto>> GetInstance(Guid id, CancellationToken cancellationToken)
    {
        var instance = await _workflowService.GetInstanceAsync(id, cancellationToken);
        return instance is null ? NotFound() : Ok(instance);
    }
}
