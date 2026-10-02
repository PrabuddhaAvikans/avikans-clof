namespace ATSolution.Application.Abstractions.Workflows;

public sealed record CostingApprovalFlowSnapshot(
    string WorkflowDefinitionId,
    string WorkflowDefinitionName,
    string WorkflowVersionId,
    int WorkflowVersionNumber,
    IReadOnlyList<CostingApprovalLevelSnapshot> Levels);
