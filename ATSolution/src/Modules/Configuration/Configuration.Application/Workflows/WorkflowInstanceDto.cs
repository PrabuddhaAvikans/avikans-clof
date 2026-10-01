namespace Configuration.Application.Workflows;

public sealed record WorkflowInstanceDto(
    Guid Id,
    string SubjectType,
    string SubjectId,
    Guid WorkflowDefinitionId,
    string WorkflowDefinitionName,
    Guid WorkflowVersionId,
    int WorkflowVersionNumber,
    string Status,
    int CurrentStepOrder,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<WorkflowInstanceStepDto> Steps);
