using Configuration.Application.Workflows;

namespace Configuration.Application.Abstractions;

public interface IWorkflowService
{
    Task<WorkflowCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default);
    Task<WorkflowDefinitionDto> UpsertDefinitionAsync(UpsertWorkflowDefinitionCommand command, CancellationToken cancellationToken = default);
    Task<WorkflowVersionDto> PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkflowInstanceDto>> ListInstancesAsync(CancellationToken cancellationToken = default);
    Task<WorkflowInstanceDto?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default);
}
