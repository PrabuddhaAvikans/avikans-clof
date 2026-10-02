namespace ATSolution.Application.Abstractions.Workflows;

public interface ICostingApprovalFlowProvider
{
    Task<CostingApprovalFlowSnapshot?> GetDefaultCostingFlowAsync(CancellationToken cancellationToken = default);
}
