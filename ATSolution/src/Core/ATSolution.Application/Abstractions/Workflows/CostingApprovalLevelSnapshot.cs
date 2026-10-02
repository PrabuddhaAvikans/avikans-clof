namespace ATSolution.Application.Abstractions.Workflows;

public sealed record CostingApprovalLevelSnapshot(
    string Id,
    string Role,
    string AssigneeName,
    string Status);
