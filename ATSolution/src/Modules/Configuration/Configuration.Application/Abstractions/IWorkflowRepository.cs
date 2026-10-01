using Configuration.Domain.Workflows;

namespace Configuration.Application.Abstractions;

public interface IWorkflowRepository
{
    Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowVersion>> ListVersionsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRule>> ListRulesAsync(CancellationToken cancellationToken = default);

    Task<WorkflowDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default);

    Task<int> GetMaxVersionNumberAsync(Guid definitionId, CancellationToken cancellationToken = default);

    Task AddVersionAsync(WorkflowVersion version, CancellationToken cancellationToken = default);

    Task<WorkflowVersion?> GetVersionAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WorkflowVersion?> FindDraftVersionAsync(Guid definitionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowVersion>> ListSiblingVersionsAsync(
        Guid definitionId,
        Guid excludeVersionId,
        CancellationToken cancellationToken = default);

    Task<WorkflowRule?> GetRuleAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddRuleAsync(WorkflowRule rule, CancellationToken cancellationToken = default);

    Task<bool> AnyDefinitionForModuleAsync(string module, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsByModuleAsync(
        string module,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowVersion>> ListVersionsByDefinitionIdsAsync(
        IReadOnlyCollection<Guid> definitionIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkflowRule>> ListRulesByDefinitionIdsAsync(
        IReadOnlyCollection<Guid> definitionIds,
        CancellationToken cancellationToken = default);

    void RemoveRules(IEnumerable<WorkflowRule> rules);

    void RemoveVersions(IEnumerable<WorkflowVersion> versions);

    void RemoveDefinitions(IEnumerable<WorkflowDefinition> definitions);

    Task<IReadOnlyList<WorkflowInstance>> ListInstancesAsync(CancellationToken cancellationToken = default);

    Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, Guid>> FindRoleIdsByNamesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken = default);
}
