namespace Configuration.Application.Workflows;

public sealed record WorkflowConditionDto(string Field, string Operator, object Value);

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

public sealed record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string Description,
    string Module,
    bool IsActive);

public sealed record WorkflowVersionDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    int VersionNumber,
    string Status,
    bool IsDefault,
    string? EffectiveFrom,
    string? EffectiveTo,
    string CreatedAt,
    string? PublishedAt,
    IReadOnlyList<WorkflowStepDefinitionDto> Steps);

public sealed record WorkflowRuleDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    string Name,
    int Priority,
    bool Enabled,
    IReadOnlyList<WorkflowConditionDto> Conditions,
    Guid WorkflowVersionId);

public sealed record WorkflowCatalogDto(
    IReadOnlyList<WorkflowDefinitionDto> Definitions,
    IReadOnlyList<WorkflowVersionDto> Versions,
    IReadOnlyList<WorkflowRuleDto> Rules);

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
    string? StartedAt,
    string? ApprovedAt,
    string? ApprovedBy,
    string? Remarks);

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
    string StartedAt,
    string? CompletedAt,
    IReadOnlyList<WorkflowInstanceStepDto> Steps);

public sealed record UpsertWorkflowDefinitionCommand(
    Guid? Id,
    string Name,
    string Description,
    string Module = "costing",
    bool IsActive = true,
    IReadOnlyList<WorkflowStepDefinitionDto>? Steps = null,
    IReadOnlyList<UpsertWorkflowRuleCommand>? Rules = null);

public sealed record UpsertWorkflowRuleCommand(
    Guid? Id,
    string Name,
    int Priority,
    bool Enabled,
    IReadOnlyList<WorkflowConditionDto> Conditions,
    Guid? WorkflowVersionId = null);
