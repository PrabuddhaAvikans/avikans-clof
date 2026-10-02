using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application.Abstractions.Workflows;
using Configuration.Application.Abstractions;
using Configuration.Application.Workflows;
using Configuration.Domain.Workflows;

namespace Configuration.Application.Services;

public sealed class CostingApprovalFlowProvider : ICostingApprovalFlowProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IWorkflowRepository _workflows;

    public CostingApprovalFlowProvider(IWorkflowRepository workflows)
    {
        _workflows = workflows;
    }

    public async Task<CostingApprovalFlowSnapshot?> GetDefaultCostingFlowAsync(
        CancellationToken cancellationToken = default)
    {
        var salesFlow = await TrySalesCostingStageAsync(cancellationToken);
        if (salesFlow is not null)
        {
            return salesFlow;
        }

        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Costing, cancellationToken);
        var definition = definitions.FirstOrDefault(item => item.IsActive) ?? definitions.FirstOrDefault();
        if (definition is null)
        {
            return null;
        }

        var versions = await _workflows.ListVersionsByDefinitionIdsAsync([definition.Id], cancellationToken);
        var version = versions.FirstOrDefault(item => item.IsDefault)
            ?? versions
                .Where(item => item.Status == WorkflowVersionStatuses.Published)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefault();

        if (version is null)
        {
            return null;
        }

        var steps = Deserialize(version.StepsJson);
        var approvals = TraverseApprovePath(steps);
        if (approvals.Count == 0)
        {
            return null;
        }

        var levels = approvals
            .Select((step, index) => new CostingApprovalLevelSnapshot(
                Guid.TryParse(step.Id, out var parsed) ? parsed.ToString() : Guid.NewGuid().ToString(),
                string.IsNullOrWhiteSpace(step.ApprovalRoleName) ? step.StepName : step.ApprovalRoleName,
                string.IsNullOrWhiteSpace(step.AssigneeName) ? "Unassigned" : step.AssigneeName!,
                index == 0 ? "pending" : "waiting"))
            .ToList();

        return new CostingApprovalFlowSnapshot(
            definition.Id.ToString(),
            definition.Name,
            version.Id.ToString(),
            version.VersionNumber,
            levels);
    }

    private async Task<CostingApprovalFlowSnapshot?> TrySalesCostingStageAsync(CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Sales, cancellationToken);
        var definition = definitions.FirstOrDefault(item => item.IsActive) ?? definitions.FirstOrDefault();
        if (definition is null)
        {
            return null;
        }

        var versions = await _workflows.ListVersionsByDefinitionIdsAsync([definition.Id], cancellationToken);
        var version = versions.FirstOrDefault(item => item.IsDefault)
            ?? versions
                .Where(item => item.Status == WorkflowVersionStatuses.Published)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefault();
        if (version is null)
        {
            return null;
        }

        var steps = Deserialize(version.StepsJson);
        if (!steps.Any(step => IsStage(step)))
        {
            return null;
        }

        var costing = steps.FirstOrDefault(step =>
            IsStage(step)
            && string.Equals(step.StageKey, WorkflowStageKeys.Costing, StringComparison.OrdinalIgnoreCase));

        var levels = (costing?.ApprovalLevels ?? [])
            .Where(level => level.IsActive)
            .OrderBy(level => level.Sequence)
            .Select((level, index) => new CostingApprovalLevelSnapshot(
                string.IsNullOrWhiteSpace(level.Id) ? Guid.NewGuid().ToString() : level.Id,
                string.IsNullOrWhiteSpace(level.AssignedRoleName) ? level.Name : level.AssignedRoleName,
                string.IsNullOrWhiteSpace(level.AssignedUserName) ? "Unassigned" : level.AssignedUserName,
                index == 0 ? "pending" : "waiting"))
            .ToList();

        return new CostingApprovalFlowSnapshot(
            definition.Id.ToString(),
            definition.Name,
            version.Id.ToString(),
            version.VersionNumber,
            levels);
    }

    private static bool IsStage(WorkflowStepDefinitionDto step) =>
        string.Equals(
            string.IsNullOrWhiteSpace(step.NodeType) ? string.Empty : step.NodeType.Trim(),
            WorkflowNodeTypes.Stage,
            StringComparison.OrdinalIgnoreCase);

    private static List<WorkflowStepDefinitionDto> TraverseApprovePath(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        if (steps.Count == 0)
        {
            return [];
        }

        var byId = steps.ToDictionary(step => step.Id, StringComparer.Ordinal);
        var start = steps.FirstOrDefault(step =>
            string.Equals(
                string.IsNullOrWhiteSpace(step.NodeType) ? WorkflowNodeTypes.Approval : step.NodeType,
                WorkflowNodeTypes.Start,
                StringComparison.OrdinalIgnoreCase));

        if (start is null || string.IsNullOrWhiteSpace(start.ApproveNextStepId))
        {
            return steps
                .Where(step =>
                    string.Equals(
                        string.IsNullOrWhiteSpace(step.NodeType) ? WorkflowNodeTypes.Approval : step.NodeType,
                        WorkflowNodeTypes.Approval,
                        StringComparison.OrdinalIgnoreCase))
                .OrderBy(step => step.StepOrder)
                .ToList();
        }

        var approvals = new List<WorkflowStepDefinitionDto>();
        var currentId = start.ApproveNextStepId;
        var guard = 0;
        while (!string.IsNullOrWhiteSpace(currentId)
            && byId.TryGetValue(currentId, out var current)
            && guard++ < steps.Count + 2)
        {
            var nodeType = string.IsNullOrWhiteSpace(current.NodeType)
                ? WorkflowNodeTypes.Approval
                : current.NodeType.Trim().ToLowerInvariant();

            if (nodeType == WorkflowNodeTypes.Completed || nodeType == WorkflowNodeTypes.Rejected)
            {
                break;
            }

            if (nodeType == WorkflowNodeTypes.Approval)
            {
                approvals.Add(current);
            }

            currentId = current.ApproveNextStepId;
        }

        return approvals;
    }

    private static WorkflowStepDefinitionDto[] Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<WorkflowStepDefinitionDto[]>(json, JsonOptions) ?? [];
        }
        catch
        {
            return [];
        }
    }
}
