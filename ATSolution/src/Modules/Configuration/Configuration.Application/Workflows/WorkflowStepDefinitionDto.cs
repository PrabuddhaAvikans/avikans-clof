namespace Configuration.Application.Workflows;

public sealed record WorkflowStepDefinitionDto(
    string Id,
    int StepOrder,
    string StepName,
    string ApprovalRoleId,
    string ApprovalRoleName,
    string? AssigneeUserId,
    string? AssigneeName,
    string ApprovalType,
    int MinApprovals);
