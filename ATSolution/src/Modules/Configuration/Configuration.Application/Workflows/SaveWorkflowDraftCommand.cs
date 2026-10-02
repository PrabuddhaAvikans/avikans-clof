namespace Configuration.Application.Workflows;

public sealed record SaveWorkflowDraftCommand(
    IReadOnlyList<WorkflowStepDefinitionDto> Steps);
