namespace Configuration.Application.Workflows;

public sealed record WorkflowInstanceStepDto(
    string Id,
    string StepDefinitionId,
    int StepOrder,
    string StepName,
    string? RoleId,
    string RoleName,
    string? AssigneeUserId,
    string AssigneeName,
    string Status,
    DateTimeOffset? StartedAt,
    DateTimeOffset? ApprovedAt,
    string? ApprovedBy,
    string? Remarks);
