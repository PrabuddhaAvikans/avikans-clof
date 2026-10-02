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
    int MinApprovals,
    string NodeType = WorkflowNodeTypes.Approval,
    string? Description = null,
    string Status = WorkflowNodeStatuses.Active,
    double? PositionX = null,
    double? PositionY = null,
    string? ApproveNextStepId = null,
    string? RejectNextStepId = null,
    string? StageKey = null,
    IReadOnlyList<WorkflowApprovalLevelDto>? ApprovalLevels = null);
