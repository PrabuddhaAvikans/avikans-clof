namespace Configuration.Application.Workflows;

public sealed record WorkflowRuleDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    string Name,
    int Priority,
    bool Enabled,
    IReadOnlyList<WorkflowConditionDto> Conditions,
    Guid WorkflowVersionId);
