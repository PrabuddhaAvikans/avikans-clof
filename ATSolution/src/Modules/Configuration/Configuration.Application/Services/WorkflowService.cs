using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using Configuration.Application.Abstractions;
using Configuration.Application.Workflows;
using Configuration.Domain.Workflows;

namespace Configuration.Application.Services;

public sealed class WorkflowService : IWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IWorkflowRepository _workflows;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public WorkflowService(
        IWorkflowRepository workflows,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _workflows = workflows;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<WorkflowCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        await EnsureCostingWorkflowAsync(cancellationToken);
        await EnsureSalesOrderWorkflowAsync(cancellationToken);
        await EnsureGraphLayoutAsync(cancellationToken);
        await EnsureStepRoleBindingsAsync(cancellationToken);
        await EnsureSalesStageRoleBindingsAsync(cancellationToken);

        var definitions = await _workflows.ListDefinitionsAsync(cancellationToken);
        var versions = await _workflows.ListVersionsAsync(cancellationToken);
        var rules = await _workflows.ListRulesAsync(cancellationToken);

        return new WorkflowCatalogDto(
            definitions.Select(MapDefinition).ToList(),
            versions.Select(MapVersion).ToList(),
            rules.Select(MapRule).ToList());
    }

    public async Task<WorkflowDefinitionDto> UpsertDefinitionAsync(
        UpsertWorkflowDefinitionCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        WorkflowDefinition definition;
        if (command.Id.HasValue)
        {
            definition = await _workflows.GetDefinitionAsync(command.Id.Value, cancellationToken)
                ?? throw new NotFoundException($"Workflow definition '{command.Id}' was not found.");
            definition.Update(command.Name, command.Description, command.Module, command.IsActive);
        }
        else
        {
            definition = WorkflowDefinition.Create(command.Name, command.Description, command.Module, command.IsActive);
            await _workflows.AddDefinitionAsync(definition, cancellationToken);
        }

        WorkflowVersion? draftVersion = null;
        if (command.Steps is not null)
        {
            var nextVersion = await _workflows.GetMaxVersionNumberAsync(definition.Id, cancellationToken);

            draftVersion = WorkflowVersion.Create(
                definition.Id,
                nextVersion + 1,
                WorkflowVersionStatuses.Draft,
                isDefault: false,
                JsonSerializer.Serialize(NormalizeSteps(command.Steps), JsonOptions));
            await _workflows.AddVersionAsync(draftVersion, cancellationToken);
        }

        if (command.Rules is not null)
        {
            foreach (var ruleCmd in command.Rules)
            {
                var versionId = ruleCmd.WorkflowVersionId
                    ?? draftVersion?.Id
                    ?? throw new ApplicationValidationException(
                    [
                        new ValidationError(nameof(ruleCmd.WorkflowVersionId), "Workflow version is required.", ValidationErrorCodes.Validation),
                    ]);

                if (ruleCmd.Id.HasValue)
                {
                    var existing = await _workflows.GetRuleAsync(ruleCmd.Id.Value, cancellationToken)
                        ?? throw new NotFoundException($"Workflow rule '{ruleCmd.Id}' was not found.");
                    existing.Update(
                        ruleCmd.Name,
                        ruleCmd.Priority,
                        ruleCmd.Enabled,
                        JsonSerializer.Serialize(ruleCmd.Conditions, JsonOptions),
                        versionId);
                }
                else
                {
                    var rule = WorkflowRule.Create(
                        definition.Id,
                        ruleCmd.Name,
                        ruleCmd.Priority,
                        ruleCmd.Enabled,
                        JsonSerializer.Serialize(ruleCmd.Conditions, JsonOptions),
                        versionId);
                    await _workflows.AddRuleAsync(rule, cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDefinition(definition);
    }

    public async Task<WorkflowCatalogDto> CreateDraftFromVersionAsync(
        Guid sourceVersionId,
        CancellationToken cancellationToken = default)
    {
        var source = await _workflows.GetVersionAsync(sourceVersionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{sourceVersionId}' was not found.");

        var copiedSteps = CopySteps(Deserialize(source.StepsJson, Array.Empty<WorkflowStepDefinitionDto>()));
        var stepsJson = JsonSerializer.Serialize(copiedSteps, JsonOptions);

        var existingDraft = await _workflows.FindDraftVersionAsync(source.WorkflowDefinitionId, cancellationToken);

        if (existingDraft is not null)
        {
            existingDraft.UpdateSteps(stepsJson);
        }
        else
        {
            var nextVersion = await _workflows.GetMaxVersionNumberAsync(source.WorkflowDefinitionId, cancellationToken);

            var draft = WorkflowVersion.Create(
                source.WorkflowDefinitionId,
                nextVersion + 1,
                WorkflowVersionStatuses.Draft,
                isDefault: false,
                stepsJson);
            await _workflows.AddVersionAsync(draft, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowCatalogDto> SaveDraftAsync(
        Guid versionId,
        SaveWorkflowDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var steps = NormalizeSteps(command.Steps);
        await _validator.ValidateAsync(command with { Steps = steps }, cancellationToken);

        var version = await _workflows.GetVersionAsync(versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        if (version.Status != WorkflowVersionStatuses.Draft)
        {
            throw InvalidState("Only a draft version can be saved.");
        }

        version.UpdateSteps(JsonSerializer.Serialize(steps, JsonOptions));
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowCatalogDto> ApplyDraftAsync(
        Guid versionId,
        ApplyWorkflowDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var steps = NormalizeSteps(command.Steps);
        await _validator.ValidateAsync(command with { Steps = steps }, cancellationToken);
        ValidateApprovalGraph(steps);

        var version = await _workflows.GetVersionAsync(versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        if (version.Status != WorkflowVersionStatuses.Draft)
        {
            throw InvalidState("Only a draft version can be applied.");
        }

        version.UpdateSteps(JsonSerializer.Serialize(steps, JsonOptions));
        await MakeDefaultAsync(version, cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowVersionDto> PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _workflows.GetVersionAsync(versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        if (version.Status == WorkflowVersionStatuses.Published && version.IsDefault)
        {
            return MapVersion(version);
        }

        EnsureHasValidGraph(version);
        await MakeDefaultAsync(version, cancellationToken);
        return MapVersion(version);
    }

    public async Task<WorkflowCatalogDto> ActivateVersionAsync(
        Guid versionId,
        CancellationToken cancellationToken = default)
    {
        var version = await _workflows.GetVersionAsync(versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        if (version.Status == WorkflowVersionStatuses.Draft)
        {
            throw InvalidState("Apply the draft to activate this new flow.");
        }

        if (version.Status == WorkflowVersionStatuses.Published && version.IsDefault)
        {
            return await GetCatalogAsync(cancellationToken);
        }

        EnsureHasValidGraph(version);
        await MakeDefaultAsync(version, cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowCatalogDto> DeleteVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _workflows.GetVersionAsync(versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        if (version.IsDefault)
        {
            throw InvalidState("The version new orders use cannot be deleted.");
        }

        var siblings = await _workflows.ListSiblingVersionsAsync(version.WorkflowDefinitionId, version.Id, cancellationToken);
        if (siblings.Count == 0)
        {
            throw InvalidState("The workflow needs at least one version.");
        }

        var rules = await _workflows.ListRulesByDefinitionIdsAsync([version.WorkflowDefinitionId], cancellationToken);
        var attached = rules.Where(rule => rule.WorkflowVersionId == version.Id).ToList();
        if (attached.Count > 0)
        {
            _workflows.RemoveRules(attached);
        }

        _workflows.RemoveVersions([version]);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowCatalogDto> ResetCatalogAsync(CancellationToken cancellationToken = default)
    {
        await RemoveModuleAsync(WorkflowModules.Costing, cancellationToken);
        await RemoveModuleAsync(WorkflowModules.Sales, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await AddCostingSeedAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await AddSalesOrderSeedAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowInstanceDto>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _workflows.ListInstancesAsync(cancellationToken);
        return items.Select(MapInstance).ToList();
    }

    public async Task<WorkflowInstanceDto?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _workflows.GetInstanceAsync(id, cancellationToken);
        return item is null ? null : MapInstance(item);
    }

    private async Task EnsureCostingWorkflowAsync(CancellationToken cancellationToken)
    {
        var exists = await _workflows.AnyDefinitionForModuleAsync(WorkflowModules.Costing, cancellationToken);
        if (exists)
        {
            return;
        }

        await AddCostingSeedAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureSalesOrderWorkflowAsync(CancellationToken cancellationToken)
    {
        var exists = await _workflows.AnyDefinitionForModuleAsync(WorkflowModules.Sales, cancellationToken);
        if (exists)
        {
            return;
        }

        await AddSalesOrderSeedAsync(cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task RemoveModuleAsync(string module, CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(module, cancellationToken);
        if (definitions.Count == 0)
        {
            return;
        }

        var definitionIds = definitions.Select(definition => definition.Id).ToList();
        var versions = await _workflows.ListVersionsByDefinitionIdsAsync(definitionIds, cancellationToken);
        var rules = await _workflows.ListRulesByDefinitionIdsAsync(definitionIds, cancellationToken);
        _workflows.RemoveRules(rules);
        _workflows.RemoveVersions(versions);
        _workflows.RemoveDefinitions(definitions);
    }

    private async Task EnsureGraphLayoutAsync(CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Costing, cancellationToken);
        if (definitions.Count == 0)
        {
            return;
        }

        var definitionIds = definitions.Select(definition => definition.Id).ToList();
        var versions = await _workflows.ListVersionsByDefinitionIdsAsync(definitionIds, cancellationToken);
        var changed = false;

        foreach (var version in versions)
        {
            var steps = Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>());
            if (steps.Length == 0 || steps.Any(step => NodeTypeOf(step) == WorkflowNodeTypes.Start))
            {
                continue;
            }

            var migrated = MigrateLinearStepsToGraph(steps);
            version.UpdateSteps(JsonSerializer.Serialize(migrated, JsonOptions));
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private static List<WorkflowStepDefinitionDto> MigrateLinearStepsToGraph(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        var approvals = steps
            .Where(IsApprovalNode)
            .OrderBy(step => step.StepOrder)
            .Select(step => step with
            {
                NodeType = WorkflowNodeTypes.Approval,
                Status = string.IsNullOrWhiteSpace(step.Status) ? WorkflowNodeStatuses.Active : step.Status,
            })
            .ToList();

        if (approvals.Count == 0)
        {
            return CreateDefaultGraphSteps();
        }

        var startId = Guid.NewGuid().ToString("N");
        var completedId = Guid.NewGuid().ToString("N");
        var rejectedId = Guid.NewGuid().ToString("N");

        var result = new List<WorkflowStepDefinitionDto>
        {
            Node(startId, 1, "Start", WorkflowNodeTypes.Start, 80, 240, approvals[0].Id, null),
        };

        for (var i = 0; i < approvals.Count; i++)
        {
            var approveNext = i < approvals.Count - 1 ? approvals[i + 1].Id : completedId;
            var x = 320 + (i * 240);
            result.Add(approvals[i] with
            {
                StepOrder = i + 2,
                PositionX = approvals[i].PositionX ?? x,
                PositionY = approvals[i].PositionY ?? 80,
                ApproveNextStepId = approveNext,
                RejectNextStepId = rejectedId,
            });
        }

        result.Add(Node(completedId, approvals.Count + 2, "Completed", WorkflowNodeTypes.Completed, 320 + (approvals.Count * 240), 240, null, null));
        result.Add(Node(rejectedId, approvals.Count + 3, "Rejected", WorkflowNodeTypes.Rejected, 320 + ((approvals.Count - 1) * 120), 360, null, null));
        return result;
    }

    private async Task EnsureStepRoleBindingsAsync(CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Costing, cancellationToken);
        if (definitions.Count == 0)
        {
            return;
        }

        var definitionIds = definitions.Select(definition => definition.Id).ToList();
        var versions = await _workflows.ListVersionsByDefinitionIdsAsync(definitionIds, cancellationToken);
        var changed = false;

        foreach (var version in versions)
        {
            var steps = Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>());
            var approvals = steps.Where(IsApprovalNode).ToArray();
            if (approvals.Length == 0 || approvals.All(step => !string.IsNullOrWhiteSpace(step.ApprovalRoleId)))
            {
                continue;
            }

            var resolved = await ResolveSeedStepsAsync(steps, cancellationToken);
            if (resolved.SequenceEqual(steps))
            {
                continue;
            }

            version.UpdateSteps(JsonSerializer.Serialize(resolved, JsonOptions));
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task AddCostingSeedAsync(CancellationToken cancellationToken)
    {
        var definition = WorkflowDefinition.Create(
            "Costing Approval",
            "Sequential costing approval. Apply a flow for new orders; running orders keep their version.",
            WorkflowModules.Costing,
            isActive: true);

        var seedSteps = CreateDefaultGraphSteps();
        var steps = await ResolveSeedStepsAsync(seedSteps, cancellationToken);

        var version = WorkflowVersion.Create(
            definition.Id,
            versionNumber: 1,
            WorkflowVersionStatuses.Draft,
            isDefault: false,
            JsonSerializer.Serialize(steps, JsonOptions));
        version.Publish();
        version.SetDefault(true);

        await _workflows.AddDefinitionAsync(definition, cancellationToken);
        await _workflows.AddVersionAsync(version, cancellationToken);
    }

    private async Task AddSalesOrderSeedAsync(CancellationToken cancellationToken)
    {
        var definition = WorkflowDefinition.Create(
            "Sales Order",
            "Quotation through delivery. Approval levels are optional inside each stage. Published versions apply to new orders; running orders keep their version.",
            WorkflowModules.Sales,
            isActive: true);

        var costingLevels = await ReadCostingApprovalLevelsAsync(cancellationToken);
        var steps = await ResolveSeedStepsAsync(CreateSalesOrderStageSteps(costingLevels), cancellationToken);
        var version = WorkflowVersion.Create(
            definition.Id,
            versionNumber: 1,
            WorkflowVersionStatuses.Draft,
            isDefault: false,
            JsonSerializer.Serialize(steps, JsonOptions));
        version.Publish();
        version.SetDefault(true);

        await _workflows.AddDefinitionAsync(definition, cancellationToken);
        await _workflows.AddVersionAsync(version, cancellationToken);
    }

    private async Task EnsureSalesStageRoleBindingsAsync(CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Sales, cancellationToken);
        if (definitions.Count == 0)
        {
            return;
        }

        var definitionIds = definitions.Select(definition => definition.Id).ToList();
        var versions = await _workflows.ListVersionsByDefinitionIdsAsync(definitionIds, cancellationToken);
        var changed = false;

        foreach (var version in versions)
        {
            var steps = Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>());
            if (!steps.Any(IsStageNode))
            {
                continue;
            }

            var needsRole = steps.Any(step =>
                step.ApprovalLevels?.Any(level =>
                    level.IsActive
                    && string.IsNullOrWhiteSpace(level.AssignedRoleId)
                    && !string.IsNullOrWhiteSpace(level.AssignedRoleName)) == true);
            if (!needsRole)
            {
                continue;
            }

            var resolved = await ResolveSeedStepsAsync(steps, cancellationToken);
            if (!StageRoleBindingsChanged(steps, resolved))
            {
                continue;
            }

            version.UpdateSteps(JsonSerializer.Serialize(resolved, JsonOptions));
            changed = true;
        }

        if (changed)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<WorkflowStepDefinitionDto[]> ResolveSeedStepsAsync(
        IReadOnlyList<WorkflowStepDefinitionDto> steps,
        CancellationToken cancellationToken)
    {
        var roleNames = steps
            .Where(IsApprovalNode)
            .Select(step => step.ApprovalRoleName)
            .Concat(steps
                .Where(step => step.ApprovalLevels is not null)
                .SelectMany(step => step.ApprovalLevels!)
                .Select(level => level.AssignedRoleName ?? string.Empty))
            .Where(name => !string.IsNullOrWhiteSpace(name));

        var roleIds = await _workflows.FindRoleIdsByNamesAsync(roleNames, cancellationToken);

        var resolved = new List<WorkflowStepDefinitionDto>(steps.Count);
        foreach (var step in steps)
        {
            if (IsStageNode(step))
            {
                resolved.Add(step with
                {
                    ApprovalLevels = ResolveLevelRoles(step.ApprovalLevels, roleIds),
                });
                continue;
            }

            if (!IsApprovalNode(step))
            {
                resolved.Add(step);
                continue;
            }

            var roleName = step.ApprovalRoleName.Trim();
            var roleId = roleIds.TryGetValue(roleName, out var id)
                ? id.ToString()
                : (step.ApprovalRoleId ?? string.Empty).Trim();

            resolved.Add(step with
            {
                ApprovalRoleId = roleId,
                ApprovalRoleName = roleName,
                StepName = string.IsNullOrWhiteSpace(step.StepName) ? roleName : step.StepName.Trim(),
            });
        }

        return resolved.ToArray();
    }

    private async Task MakeDefaultAsync(WorkflowVersion version, CancellationToken cancellationToken)
    {
        var siblings = await _workflows.ListSiblingVersionsAsync(
            version.WorkflowDefinitionId,
            version.Id,
            cancellationToken);

        foreach (var sibling in siblings.Where(sibling => sibling.IsDefault))
        {
            sibling.SetDefault(false);
        }

        if (version.Status != WorkflowVersionStatuses.Published)
        {
            version.Publish();
        }

        version.SetDefault(true);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private void EnsureHasValidGraph(WorkflowVersion version)
    {
        var steps = NormalizeSteps(Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>()));
        ValidateApprovalGraph(steps);
    }

    internal static void ValidateApprovalGraph(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        if (steps.Count == 0)
        {
            throw InvalidState("A workflow version needs at least one stage.");
        }

        if (steps.Any(IsStageNode))
        {
            ValidateStageFlow(steps);
            return;
        }

        var byId = steps.ToDictionary(step => step.Id, StringComparer.Ordinal);
        var starts = steps.Where(step => NodeTypeOf(step) == WorkflowNodeTypes.Start).ToList();
        var completed = steps.Where(step => NodeTypeOf(step) == WorkflowNodeTypes.Completed).ToList();
        var rejected = steps.Where(step => NodeTypeOf(step) == WorkflowNodeTypes.Rejected).ToList();
        var approvals = steps.Where(IsApprovalNode).ToList();

        if (starts.Count != 1)
        {
            throw InvalidState("Workflow must have exactly one Start node.");
        }

        if (completed.Count != 1)
        {
            throw InvalidState("Workflow must have exactly one Completed node.");
        }

        if (rejected.Count != 1)
        {
            throw InvalidState("Workflow must have exactly one Rejected node.");
        }

        if (approvals.Count == 0)
        {
            throw InvalidState("Workflow must have at least one approval step.");
        }

        foreach (var approval in approvals)
        {
            if (string.IsNullOrWhiteSpace(approval.ApprovalRoleId) || string.IsNullOrWhiteSpace(approval.ApprovalRoleName))
            {
                throw InvalidState($"Approval step '{approval.StepName}' requires an assigned role.");
            }

            if (string.IsNullOrWhiteSpace(approval.ApproveNextStepId) || !byId.ContainsKey(approval.ApproveNextStepId))
            {
                throw InvalidState($"Approval step '{approval.StepName}' needs a valid Approve connection.");
            }

            if (string.IsNullOrWhiteSpace(approval.RejectNextStepId) || !byId.ContainsKey(approval.RejectNextStepId))
            {
                throw InvalidState($"Approval step '{approval.StepName}' needs a valid Reject connection.");
            }

            if (!ReachesNodeType(approval.RejectNextStepId, WorkflowNodeTypes.Rejected, byId, new HashSet<string>(StringComparer.Ordinal)))
            {
                throw InvalidState($"Approval step '{approval.StepName}' reject path must reach Rejected.");
            }
        }

        var start = starts[0];
        if (string.IsNullOrWhiteSpace(start.ApproveNextStepId) || !byId.ContainsKey(start.ApproveNextStepId))
        {
            throw InvalidState("Start must connect to the first approval step.");
        }

        var approveVisited = new HashSet<string>(StringComparer.Ordinal);
        if (!ReachesNodeType(start.Id, WorkflowNodeTypes.Completed, byId, approveVisited, followApproveOnly: true))
        {
            throw InvalidState("Approve path from Start must reach Completed.");
        }

        if (!approveVisited.Overlaps(approvals.Select(step => step.Id)))
        {
            throw InvalidState("Start must connect into the approval chain.");
        }

        var reachable = CollectReachable(start.Id, byId);
        var disconnected = steps.Where(step => !reachable.Contains(step.Id)).ToList();
        if (disconnected.Count > 0)
        {
            throw InvalidState($"Disconnected steps: {string.Join(", ", disconnected.Select(step => step.StepName))}.");
        }
    }

    private static bool ReachesNodeType(
        string startId,
        string targetType,
        IReadOnlyDictionary<string, WorkflowStepDefinitionDto> byId,
        HashSet<string> visited,
        bool followApproveOnly = false)
    {
        if (!byId.TryGetValue(startId, out var current))
        {
            return false;
        }

        if (!visited.Add(current.Id))
        {
            return false;
        }

        if (NodeTypeOf(current) == targetType)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(current.ApproveNextStepId)
            && ReachesNodeType(current.ApproveNextStepId, targetType, byId, visited, followApproveOnly))
        {
            return true;
        }

        if (!followApproveOnly
            && !string.IsNullOrWhiteSpace(current.RejectNextStepId)
            && ReachesNodeType(current.RejectNextStepId, targetType, byId, visited, followApproveOnly))
        {
            return true;
        }

        return false;
    }

    private static HashSet<string> CollectReachable(
        string startId,
        IReadOnlyDictionary<string, WorkflowStepDefinitionDto> byId)
    {
        var reachable = new HashSet<string>(StringComparer.Ordinal);
        var queue = new Queue<string>();
        queue.Enqueue(startId);

        while (queue.Count > 0)
        {
            var id = queue.Dequeue();
            if (!reachable.Add(id) || !byId.TryGetValue(id, out var step))
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(step.ApproveNextStepId))
            {
                queue.Enqueue(step.ApproveNextStepId);
            }

            if (!string.IsNullOrWhiteSpace(step.RejectNextStepId))
            {
                queue.Enqueue(step.RejectNextStepId);
            }
        }

        return reachable;
    }

    private static List<WorkflowStepDefinitionDto> CopySteps(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        var idMap = steps.ToDictionary(
            step => step.Id,
            _ => Guid.NewGuid().ToString("N"),
            StringComparer.Ordinal);

        string? MapNext(string? nextId) =>
            string.IsNullOrWhiteSpace(nextId)
                ? null
                : idMap.TryGetValue(nextId, out var mapped) ? mapped : null;

        return steps.Select((step, index) => step with
        {
            Id = idMap[step.Id],
            StepOrder = index + 1,
            ApproveNextStepId = MapNext(step.ApproveNextStepId),
            RejectNextStepId = MapNext(step.RejectNextStepId),
            ApprovalLevels = CopyLevels(step.ApprovalLevels),
        }).ToList();
    }

    private static IReadOnlyList<WorkflowApprovalLevelDto>? CopyLevels(IReadOnlyList<WorkflowApprovalLevelDto>? levels)
    {
        if (levels is null)
        {
            return null;
        }

        return levels
            .Select(level => level with { Id = Guid.NewGuid().ToString("N") })
            .ToList();
    }

    private static List<WorkflowStepDefinitionDto> NormalizeSteps(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        var normalized = steps.Select((step, index) =>
        {
            var nodeType = string.IsNullOrWhiteSpace(step.NodeType)
                ? WorkflowNodeTypes.Approval
                : step.NodeType.Trim().ToLowerInvariant();
            var roleName = (step.ApprovalRoleName ?? string.Empty).Trim();
            var stepName = string.IsNullOrWhiteSpace(step.StepName)
                ? DefaultNameFor(nodeType, roleName, index)
                : step.StepName.Trim();

            return step with
            {
                Id = string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString("N") : step.Id.Trim(),
                StepOrder = index + 1,
                StepName = stepName,
                ApprovalRoleId = IsApprovalNodeType(nodeType)
                    ? (step.ApprovalRoleId ?? string.Empty).Trim()
                    : string.Empty,
                ApprovalRoleName = IsApprovalNodeType(nodeType) ? roleName : string.Empty,
                ApprovalType = string.IsNullOrWhiteSpace(step.ApprovalType) ? "sequential" : step.ApprovalType.Trim(),
                MinApprovals = step.MinApprovals < 1 ? 1 : step.MinApprovals,
                AssigneeUserId = string.IsNullOrWhiteSpace(step.AssigneeUserId) ? null : step.AssigneeUserId.Trim(),
                AssigneeName = string.IsNullOrWhiteSpace(step.AssigneeName) ? null : step.AssigneeName.Trim(),
                NodeType = nodeType,
                Description = string.IsNullOrWhiteSpace(step.Description) ? null : step.Description.Trim(),
                Status = string.IsNullOrWhiteSpace(step.Status) ? WorkflowNodeStatuses.Active : step.Status.Trim().ToLowerInvariant(),
                PositionX = step.PositionX,
                PositionY = step.PositionY,
                ApproveNextStepId = string.IsNullOrWhiteSpace(step.ApproveNextStepId) ? null : step.ApproveNextStepId.Trim(),
                RejectNextStepId = string.IsNullOrWhiteSpace(step.RejectNextStepId) ? null : step.RejectNextStepId.Trim(),
                StageKey = string.IsNullOrWhiteSpace(step.StageKey) ? null : step.StageKey.Trim().ToLowerInvariant(),
                ApprovalLevels = nodeType == WorkflowNodeTypes.Stage
                    ? NormalizeLevels(step.ApprovalLevels)
                    : null,
            };
        }).ToList();

        if (normalized.Any(IsStageNode))
        {
            return LinkStages(normalized);
        }

        return AssignApprovePathOrder(normalized);
    }

    private static List<WorkflowApprovalLevelDto> NormalizeLevels(IReadOnlyList<WorkflowApprovalLevelDto>? levels)
    {
        if (levels is null || levels.Count == 0)
        {
            return [];
        }

        return levels.Select((level, index) => new WorkflowApprovalLevelDto(
            string.IsNullOrWhiteSpace(level.Id) ? Guid.NewGuid().ToString("N") : level.Id.Trim(),
            string.IsNullOrWhiteSpace(level.Name) ? $"Level {index + 1}" : level.Name.Trim(),
            index + 1,
            (level.AssignedRoleId ?? string.Empty).Trim(),
            string.IsNullOrWhiteSpace(level.AssignedRoleName) ? null : level.AssignedRoleName.Trim(),
            string.IsNullOrWhiteSpace(level.AssignedUserId) ? null : level.AssignedUserId.Trim(),
            string.IsNullOrWhiteSpace(level.AssignedUserName) ? null : level.AssignedUserName.Trim(),
            string.IsNullOrWhiteSpace(level.Description) ? null : level.Description.Trim(),
            level.IsActive)).ToList();
    }

    private static List<WorkflowStepDefinitionDto> LinkStages(List<WorkflowStepDefinitionDto> steps)
    {
        var stageIndexes = steps
            .Select((step, index) => (step, index))
            .Where(item => IsStageNode(item.step))
            .ToList();

        for (var i = 0; i < stageIndexes.Count; i++)
        {
            var nextId = i < stageIndexes.Count - 1 ? stageIndexes[i + 1].step.Id : null;
            var index = stageIndexes[i].index;
            steps[index] = steps[index] with
            {
                ApproveNextStepId = nextId,
                RejectNextStepId = null,
            };
        }

        return steps;
    }

    private static void ValidateStageFlow(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        if (steps.Any(step => !IsStageNode(step)))
        {
            throw InvalidState("A stage workflow only contains business stages.");
        }

        var stages = steps.Where(IsStageNode).OrderBy(step => step.StepOrder).ToList();
        if (stages.Count < 2)
        {
            throw InvalidState("The workflow needs a start stage and a final stage.");
        }

        var keys = stages
            .Select(step => step.StageKey ?? string.Empty)
            .Where(key => !string.IsNullOrWhiteSpace(key))
            .ToList();
        if (keys.Count != keys.Distinct(StringComparer.OrdinalIgnoreCase).Count())
        {
            throw InvalidState("Each business stage can appear only once.");
        }

        if (string.Equals(stages[0].StageKey, WorkflowStageKeys.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidState("Completed cannot be the first stage.");
        }

        if (!string.Equals(stages[^1].StageKey, WorkflowStageKeys.Completed, StringComparison.OrdinalIgnoreCase))
        {
            throw InvalidState("The workflow must end at the Completed stage.");
        }

        var byId = stages.ToDictionary(step => step.Id, StringComparer.Ordinal);
        for (var i = 0; i < stages.Count - 1; i++)
        {
            var nextId = stages[i].ApproveNextStepId;
            if (string.IsNullOrWhiteSpace(nextId) || !byId.TryGetValue(nextId, out var next) || next.Id != stages[i + 1].Id)
            {
                throw InvalidState($"Stage '{stages[i].StepName}' must connect to the next stage.");
            }
        }

        if (!string.IsNullOrWhiteSpace(stages[^1].ApproveNextStepId))
        {
            throw InvalidState("The Completed stage is the end of the flow.");
        }

        foreach (var stage in stages)
        {
            var levels = stage.ApprovalLevels ?? [];
            if (string.Equals(stage.StageKey, WorkflowStageKeys.Completed, StringComparison.OrdinalIgnoreCase) && levels.Count > 0)
            {
                throw InvalidState("Completed does not take approval levels.");
            }

            var sequences = levels.Select(level => level.Sequence).ToList();
            if (sequences.Count != sequences.Distinct().Count())
            {
                throw InvalidState($"Stage '{stage.StepName}' has duplicate approval sequences.");
            }

            foreach (var level in levels.Where(level => level.IsActive))
            {
                var hasApprover = !string.IsNullOrWhiteSpace(level.AssignedRoleId)
                    || !string.IsNullOrWhiteSpace(level.AssignedRoleName)
                    || !string.IsNullOrWhiteSpace(level.AssignedUserId);
                if (!hasApprover)
                {
                    throw InvalidState($"Approval level '{level.Name}' on '{stage.StepName}' needs an assigned approver.");
                }
            }
        }
    }

    private static List<WorkflowStepDefinitionDto> AssignApprovePathOrder(List<WorkflowStepDefinitionDto> steps)
    {
        var byId = steps.ToDictionary(step => step.Id, StringComparer.Ordinal);
        var start = steps.FirstOrDefault(step => NodeTypeOf(step) == WorkflowNodeTypes.Start);
        if (start is null || string.IsNullOrWhiteSpace(start.ApproveNextStepId))
        {
            return steps;
        }

        var orderedIds = new List<string> { start.Id };
        var currentId = start.ApproveNextStepId;
        var guard = 0;
        while (!string.IsNullOrWhiteSpace(currentId) && byId.TryGetValue(currentId, out var current) && guard++ < steps.Count + 2)
        {
            orderedIds.Add(current.Id);
            if (NodeTypeOf(current) == WorkflowNodeTypes.Completed)
            {
                break;
            }

            currentId = current.ApproveNextStepId;
        }

        foreach (var step in steps.Where(step => !orderedIds.Contains(step.Id, StringComparer.Ordinal)))
        {
            orderedIds.Add(step.Id);
        }

        return orderedIds
            .Select((id, index) => byId[id] with { StepOrder = index + 1 })
            .ToList();
    }

    private static List<WorkflowStepDefinitionDto> CreateDefaultGraphSteps()
    {
        var startId = Guid.NewGuid().ToString("N");
        var step1Id = Guid.NewGuid().ToString("N");
        var step2Id = Guid.NewGuid().ToString("N");
        var step3Id = Guid.NewGuid().ToString("N");
        var completedId = Guid.NewGuid().ToString("N");
        var rejectedId = Guid.NewGuid().ToString("N");

        return
        [
            Node(startId, 1, "Start", WorkflowNodeTypes.Start, 80, 240, step1Id, null),
            ApprovalNode(step1Id, 2, "Production Manager", 320, 80, step2Id, rejectedId),
            ApprovalNode(step2Id, 3, "Sales Manager", 560, 80, step3Id, rejectedId),
            ApprovalNode(step3Id, 4, "Administrator", 800, 80, completedId, rejectedId),
            Node(completedId, 5, "Completed", WorkflowNodeTypes.Completed, 1040, 240, null, null),
            Node(rejectedId, 6, "Rejected", WorkflowNodeTypes.Rejected, 560, 360, null, null),
        ];
    }

    private static WorkflowStepDefinitionDto ApprovalNode(
        string id,
        int order,
        string roleName,
        double x,
        double y,
        string? approveNext,
        string? rejectNext) =>
        new(
            id,
            order,
            roleName,
            string.Empty,
            roleName,
            null,
            null,
            "sequential",
            1,
            WorkflowNodeTypes.Approval,
            null,
            WorkflowNodeStatuses.Active,
            x,
            y,
            approveNext,
            rejectNext);

    private static WorkflowStepDefinitionDto Node(
        string id,
        int order,
        string name,
        string nodeType,
        double x,
        double y,
        string? approveNext,
        string? rejectNext) =>
        new(
            id,
            order,
            name,
            string.Empty,
            string.Empty,
            null,
            null,
            "sequential",
            1,
            nodeType,
            null,
            WorkflowNodeStatuses.Active,
            x,
            y,
            approveNext,
            rejectNext);

    private static string DefaultNameFor(string nodeType, string roleName, int index) =>
        nodeType switch
        {
            WorkflowNodeTypes.Start => "Start",
            WorkflowNodeTypes.Completed => "Completed",
            WorkflowNodeTypes.Rejected => "Rejected",
            _ => string.IsNullOrWhiteSpace(roleName) ? $"Step {index + 1}" : roleName,
        };

    private async Task<List<WorkflowApprovalLevelDto>> ReadCostingApprovalLevelsAsync(
        CancellationToken cancellationToken)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Costing, cancellationToken);
        var definition = definitions.FirstOrDefault(item => item.IsActive) ?? definitions.FirstOrDefault();
        if (definition is null)
        {
            return DefaultCostingLevels();
        }

        var versions = await _workflows.ListVersionsByDefinitionIdsAsync([definition.Id], cancellationToken);
        var version = versions.FirstOrDefault(item => item.IsDefault)
            ?? versions
                .Where(item => item.Status == WorkflowVersionStatuses.Published)
                .OrderByDescending(item => item.VersionNumber)
                .FirstOrDefault();
        if (version is null)
        {
            return DefaultCostingLevels();
        }

        var steps = Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>());
        var approvals = ApprovalPath(steps);
        if (approvals.Count == 0)
        {
            return DefaultCostingLevels();
        }

        return approvals.Select((step, index) => new WorkflowApprovalLevelDto(
            Guid.NewGuid().ToString("N"),
            string.IsNullOrWhiteSpace(step.StepName) ? step.ApprovalRoleName : step.StepName,
            index + 1,
            step.ApprovalRoleId ?? string.Empty,
            step.ApprovalRoleName,
            step.AssigneeUserId,
            step.AssigneeName,
            step.Description,
            !string.Equals(step.Status, WorkflowNodeStatuses.Inactive, StringComparison.OrdinalIgnoreCase))).ToList();
    }

    private static List<WorkflowStepDefinitionDto> ApprovalPath(IReadOnlyList<WorkflowStepDefinitionDto> steps)
    {
        var byId = steps.ToDictionary(step => step.Id, StringComparer.Ordinal);
        var start = steps.FirstOrDefault(step => NodeTypeOf(step) == WorkflowNodeTypes.Start);
        if (start is null || string.IsNullOrWhiteSpace(start.ApproveNextStepId))
        {
            return steps.Where(IsApprovalNode).OrderBy(step => step.StepOrder).ToList();
        }

        var approvals = new List<WorkflowStepDefinitionDto>();
        var currentId = start.ApproveNextStepId;
        var guard = 0;
        while (!string.IsNullOrWhiteSpace(currentId)
            && byId.TryGetValue(currentId, out var current)
            && guard++ < steps.Count + 2)
        {
            if (NodeTypeOf(current) is WorkflowNodeTypes.Completed or WorkflowNodeTypes.Rejected)
            {
                break;
            }

            if (IsApprovalNode(current))
            {
                approvals.Add(current);
            }

            currentId = current.ApproveNextStepId;
        }

        return approvals;
    }

    private static List<WorkflowApprovalLevelDto> DefaultCostingLevels() =>
    [
        ApprovalLevel("Production Manager", "Production Manager", 1),
        ApprovalLevel("Sales Manager", "Sales Manager", 2),
        ApprovalLevel("Administrator", "Administrator", 3),
    ];

    private static List<WorkflowStepDefinitionDto> CreateSalesOrderStageSteps(
        IReadOnlyList<WorkflowApprovalLevelDto> costingLevels)
    {
        (string Key, string Name)[] stages =
        [
            (WorkflowStageKeys.Quotation, "Quotation"),
            (WorkflowStageKeys.SalesOrder, "Sales Order"),
            (WorkflowStageKeys.Estimation, "Estimation"),
            (WorkflowStageKeys.Costing, "Costing"),
            (WorkflowStageKeys.Production, "Production"),
            (WorkflowStageKeys.Delivery, "Delivery"),
            (WorkflowStageKeys.Completed, "Completed"),
        ];

        var ids = stages.Select(_ => Guid.NewGuid().ToString("N")).ToArray();
        var steps = new List<WorkflowStepDefinitionDto>(stages.Length);
        for (var i = 0; i < stages.Length; i++)
        {
            var levels = stages[i].Key == WorkflowStageKeys.Costing
                ? costingLevels.ToList()
                : [];

            steps.Add(StageNode(
                ids[i],
                i + 1,
                stages[i].Name,
                stages[i].Key,
                i < stages.Length - 1 ? ids[i + 1] : null,
                levels));
        }

        return steps;
    }

    private static WorkflowApprovalLevelDto ApprovalLevel(string name, string roleName, int sequence) =>
        new(
            Guid.NewGuid().ToString("N"),
            name,
            sequence,
            string.Empty,
            roleName,
            null,
            null,
            null,
            true);

    private static WorkflowStepDefinitionDto StageNode(
        string id,
        int order,
        string name,
        string stageKey,
        string? approveNext,
        IReadOnlyList<WorkflowApprovalLevelDto> levels) =>
        new(
            id,
            order,
            name,
            string.Empty,
            string.Empty,
            null,
            null,
            "sequential",
            1,
            WorkflowNodeTypes.Stage,
            null,
            WorkflowNodeStatuses.Active,
            280,
            order * 160,
            approveNext,
            null,
            stageKey,
            levels);

    private static IReadOnlyList<WorkflowApprovalLevelDto>? ResolveLevelRoles(
        IReadOnlyList<WorkflowApprovalLevelDto>? levels,
        IReadOnlyDictionary<string, Guid> roleIds)
    {
        if (levels is null)
        {
            return null;
        }

        return levels.Select(level =>
        {
            var roleName = (level.AssignedRoleName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(roleName) || !string.IsNullOrWhiteSpace(level.AssignedRoleId))
            {
                return level;
            }

            var roleId = roleIds.TryGetValue(roleName, out var id)
                ? id.ToString()
                : level.AssignedRoleId;
            return level with { AssignedRoleId = roleId, AssignedRoleName = roleName };
        }).ToList();
    }

    private static bool StageRoleBindingsChanged(
        IReadOnlyList<WorkflowStepDefinitionDto> before,
        IReadOnlyList<WorkflowStepDefinitionDto> after)
    {
        if (before.Count != after.Count)
        {
            return true;
        }

        for (var i = 0; i < before.Count; i++)
        {
            var left = before[i].ApprovalLevels ?? [];
            var right = after[i].ApprovalLevels ?? [];
            if (left.Count != right.Count)
            {
                return true;
            }

            for (var j = 0; j < left.Count; j++)
            {
                if (!string.Equals(left[j].AssignedRoleId, right[j].AssignedRoleId, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsStageNode(WorkflowStepDefinitionDto step) =>
        string.Equals(NodeTypeOf(step), WorkflowNodeTypes.Stage, StringComparison.OrdinalIgnoreCase);

    private static bool IsApprovalNode(WorkflowStepDefinitionDto step) =>
        IsApprovalNodeType(NodeTypeOf(step));

    private static bool IsApprovalNodeType(string nodeType) =>
        string.Equals(nodeType, WorkflowNodeTypes.Approval, StringComparison.OrdinalIgnoreCase);

    private static string NodeTypeOf(WorkflowStepDefinitionDto step) =>
        string.IsNullOrWhiteSpace(step.NodeType) ? WorkflowNodeTypes.Approval : step.NodeType.Trim().ToLowerInvariant();

    private static ApplicationValidationException InvalidState(string message) =>
        new(
        [
            new ValidationError(nameof(WorkflowVersion), message, ValidationErrorCodes.InvalidState),
        ]);

    private static WorkflowDefinitionDto MapDefinition(WorkflowDefinition d) =>
        new(d.Id, d.Name, d.Description, d.Module, d.IsActive);

    private WorkflowVersionDto MapVersion(WorkflowVersion v) =>
        new(
            v.Id,
            v.WorkflowDefinitionId,
            v.VersionNumber,
            v.Status,
            v.IsDefault,
            v.EffectiveFrom,
            v.EffectiveTo,
            v.CreatedOnUtc,
            v.PublishedAtUtc,
            Deserialize(v.StepsJson, Array.Empty<WorkflowStepDefinitionDto>()));

    private WorkflowRuleDto MapRule(WorkflowRule r) =>
        new(
            r.Id,
            r.WorkflowDefinitionId,
            r.Name,
            r.Priority,
            r.Enabled,
            Deserialize(r.ConditionsJson, Array.Empty<WorkflowConditionDto>()),
            r.WorkflowVersionId);

    private WorkflowInstanceDto MapInstance(WorkflowInstance i) =>
        new(
            i.Id,
            i.SubjectType,
            i.SubjectId,
            i.WorkflowDefinitionId,
            i.WorkflowDefinitionName,
            i.WorkflowVersionId,
            i.WorkflowVersionNumber,
            i.Status,
            i.CurrentStepOrder,
            i.StartedAtUtc,
            i.CompletedAtUtc,
            Deserialize(i.StepsJson, Array.Empty<WorkflowInstanceStepDto>()));

    private static T Deserialize<T>(string? json, T fallback)
    {
        if (string.IsNullOrWhiteSpace(json)) return fallback;
        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch
        {
            return fallback;
        }
    }
}
