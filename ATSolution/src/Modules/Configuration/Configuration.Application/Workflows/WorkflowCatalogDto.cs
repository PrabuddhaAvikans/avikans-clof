namespace Configuration.Application.Workflows;

public sealed record WorkflowCatalogDto(
    IReadOnlyList<WorkflowDefinitionDto> Definitions,
    IReadOnlyList<WorkflowVersionDto> Versions,
    IReadOnlyList<WorkflowRuleDto> Rules);
