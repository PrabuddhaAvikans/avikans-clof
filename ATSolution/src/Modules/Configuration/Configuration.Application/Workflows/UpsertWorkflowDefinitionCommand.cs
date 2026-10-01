namespace Configuration.Application.Workflows;

public sealed record UpsertWorkflowDefinitionCommand(
    Guid? Id,
    string Name,
    string Description,
    string Module = "costing",
    bool IsActive = true,
    IReadOnlyList<WorkflowStepDefinitionDto>? Steps = null,
    IReadOnlyList<UpsertWorkflowRuleCommand>? Rules = null);
