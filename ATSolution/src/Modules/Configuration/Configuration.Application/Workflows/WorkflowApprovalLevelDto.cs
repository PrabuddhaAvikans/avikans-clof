namespace Configuration.Application.Workflows;

public sealed record WorkflowApprovalLevelDto(
    string Id,
    string Name,
    int Sequence,
    string AssignedRoleId,
    string? AssignedRoleName,
    string? AssignedUserId,
    string? AssignedUserName,
    string? Description,
    bool IsActive);
