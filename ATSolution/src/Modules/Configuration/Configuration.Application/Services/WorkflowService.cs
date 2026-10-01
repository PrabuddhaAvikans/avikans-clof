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
        await EnsureStepRoleBindingsAsync(cancellationToken);

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
                JsonSerializer.Serialize(command.Steps, JsonOptions));
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

    public async Task<WorkflowCatalogDto> ApplyDraftAsync(
        Guid versionId,
        ApplyWorkflowDraftCommand command,
        CancellationToken cancellationToken = default)
    {
        var steps = NormalizeSteps(command.Steps);
        await _validator.ValidateAsync(command with { Steps = steps }, cancellationToken);

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

        EnsureHasSteps(version);
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

        EnsureHasSteps(version);
        await MakeDefaultAsync(version, cancellationToken);
        return await GetCatalogAsync(cancellationToken);
    }

    public async Task<WorkflowCatalogDto> ResetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _workflows.ListDefinitionsByModuleAsync(WorkflowModules.Costing, cancellationToken);
        var definitionIds = definitions.Select(d => d.Id).ToList();

        if (definitionIds.Count > 0)
        {
            var versions = await _workflows.ListVersionsByDefinitionIdsAsync(definitionIds, cancellationToken);
            var rules = await _workflows.ListRulesByDefinitionIdsAsync(definitionIds, cancellationToken);

            _workflows.RemoveRules(rules);
            _workflows.RemoveVersions(versions);
            _workflows.RemoveDefinitions(definitions);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await AddCostingSeedAsync(cancellationToken);
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
            if (steps.Length == 0 || steps.All(step => !string.IsNullOrWhiteSpace(step.ApprovalRoleId)))
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

        var seedSteps = new[]
        {
            SeedStep(1, "Production Manager"),
            SeedStep(2, "Sales Manager"),
            SeedStep(3, "Administrator"),
        };
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

    private async Task<WorkflowStepDefinitionDto[]> ResolveSeedStepsAsync(
        IReadOnlyList<WorkflowStepDefinitionDto> steps,
        CancellationToken cancellationToken)
    {
        var roleIds = await _workflows.FindRoleIdsByNamesAsync(
            steps.Select(step => step.ApprovalRoleName),
            cancellationToken);

        var resolved = new List<WorkflowStepDefinitionDto>(steps.Count);
        foreach (var step in steps)
        {
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

    private void EnsureHasSteps(WorkflowVersion version)
    {
        var steps = Deserialize(version.StepsJson, Array.Empty<WorkflowStepDefinitionDto>());
        if (steps.Length == 0)
        {
            throw InvalidState("A workflow version needs at least one approval step.");
        }
    }

    private static List<WorkflowStepDefinitionDto> CopySteps(IReadOnlyList<WorkflowStepDefinitionDto> steps) =>
        steps.Select((step, index) => step with
        {
            Id = Guid.NewGuid().ToString("N"),
            StepOrder = index + 1,
        }).ToList();

    private static List<WorkflowStepDefinitionDto> NormalizeSteps(IReadOnlyList<WorkflowStepDefinitionDto> steps) =>
        steps.Select((step, index) =>
        {
            var roleName = (step.ApprovalRoleName ?? string.Empty).Trim();
            var stepName = string.IsNullOrWhiteSpace(step.StepName) ? roleName : step.StepName.Trim();
            return step with
            {
                Id = string.IsNullOrWhiteSpace(step.Id) ? Guid.NewGuid().ToString("N") : step.Id.Trim(),
                StepOrder = index + 1,
                StepName = stepName,
                ApprovalRoleId = (step.ApprovalRoleId ?? string.Empty).Trim(),
                ApprovalRoleName = roleName,
                ApprovalType = string.IsNullOrWhiteSpace(step.ApprovalType) ? "sequential" : step.ApprovalType.Trim(),
                MinApprovals = step.MinApprovals < 1 ? 1 : step.MinApprovals,
                AssigneeUserId = string.IsNullOrWhiteSpace(step.AssigneeUserId) ? null : step.AssigneeUserId.Trim(),
                AssigneeName = string.IsNullOrWhiteSpace(step.AssigneeName) ? null : step.AssigneeName.Trim(),
            };
        }).ToList();

    private static WorkflowStepDefinitionDto SeedStep(int order, string roleName) =>
        new(
            Guid.NewGuid().ToString("N"),
            order,
            roleName,
            string.Empty,
            roleName,
            null,
            null,
            "sequential",
            1);

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
