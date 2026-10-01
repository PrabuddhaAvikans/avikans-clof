namespace Configuration.Application.Workflows;

public sealed record ApplyWorkflowDraftCommand(
    IReadOnlyList<WorkflowStepDefinitionDto> Steps);
