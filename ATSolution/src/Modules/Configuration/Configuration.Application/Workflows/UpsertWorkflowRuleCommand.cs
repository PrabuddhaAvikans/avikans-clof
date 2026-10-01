namespace Configuration.Application.Workflows;

public sealed record UpsertWorkflowRuleCommand(
    Guid? Id,
    string Name,
    int Priority,
    bool Enabled,
    IReadOnlyList<WorkflowConditionDto> Conditions,
    Guid? WorkflowVersionId = null);
