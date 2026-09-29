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
using Microsoft.EntityFrameworkCore;

namespace Configuration.Application.Services;

public sealed class WorkflowService : IWorkflowService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRepository<WorkflowDefinition, Guid> _definitions;
    private readonly IRepository<WorkflowVersion, Guid> _versions;
    private readonly IRepository<WorkflowRule, Guid> _rules;
    private readonly IRepository<WorkflowInstance, Guid> _instances;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public WorkflowService(
        IRepository<WorkflowDefinition, Guid> definitions,
        IRepository<WorkflowVersion, Guid> versions,
        IRepository<WorkflowRule, Guid> rules,
        IRepository<WorkflowInstance, Guid> instances,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _definitions = definitions;
        _versions = versions;
        _rules = rules;
        _instances = instances;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<WorkflowCatalogDto> GetCatalogAsync(CancellationToken cancellationToken = default)
    {
        var definitions = await _definitions.Query().AsNoTracking().OrderBy(d => d.Name).ToListAsync(cancellationToken);
        var versions = await _versions.Query().AsNoTracking().OrderBy(v => v.VersionNumber).ToListAsync(cancellationToken);
        var rules = await _rules.Query().AsNoTracking().OrderBy(r => r.Priority).ToListAsync(cancellationToken);

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
            definition = await _definitions.Query()
                .FirstOrDefaultAsync(d => d.Id == command.Id.Value, cancellationToken)
                ?? throw new NotFoundException($"Workflow definition '{command.Id}' was not found.");
            definition.Update(command.Name, command.Description, command.Module, command.IsActive);
        }
        else
        {
            definition = WorkflowDefinition.Create(command.Name, command.Description, command.Module, command.IsActive);
            await _definitions.AddAsync(definition, cancellationToken);
        }

        WorkflowVersion? draftVersion = null;
        if (command.Steps is not null)
        {
            var nextVersion = await _versions.Query()
                .Where(v => v.WorkflowDefinitionId == definition.Id)
                .Select(v => (int?)v.VersionNumber)
                .MaxAsync(cancellationToken) ?? 0;

            draftVersion = WorkflowVersion.Create(
                definition.Id,
                nextVersion + 1,
                WorkflowVersionStatuses.Draft,
                isDefault: false,
                JsonSerializer.Serialize(command.Steps, JsonOptions));
            await _versions.AddAsync(draftVersion, cancellationToken);
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
                    var existing = await _rules.Query()
                        .FirstOrDefaultAsync(r => r.Id == ruleCmd.Id.Value, cancellationToken)
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
                    await _rules.AddAsync(rule, cancellationToken);
                }
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapDefinition(definition);
    }

    public async Task<WorkflowVersionDto> PublishVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        var version = await _versions.Query()
            .FirstOrDefaultAsync(v => v.Id == versionId, cancellationToken)
            ?? throw new NotFoundException($"Workflow version '{versionId}' was not found.");

        var siblings = await _versions.Query()
            .Where(v => v.WorkflowDefinitionId == version.WorkflowDefinitionId && v.Id != version.Id)
            .ToListAsync(cancellationToken);

        foreach (var sibling in siblings.Where(s => s.Status == WorkflowVersionStatuses.Published && s.IsDefault))
        {
            sibling.SetDefault(false);
            sibling.Retire();
        }

        version.Publish();
        version.SetDefault(true);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return MapVersion(version);
    }

    public async Task<IReadOnlyList<WorkflowInstanceDto>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        var items = await _instances.Query().AsNoTracking()
            .OrderByDescending(i => i.StartedAtUtc)
            .ToListAsync(cancellationToken);
        return items.Select(MapInstance).ToList();
    }

    public async Task<WorkflowInstanceDto?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _instances.Query().AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        return item is null ? null : MapInstance(item);
    }

    private static WorkflowDefinitionDto MapDefinition(WorkflowDefinition d) =>
        new(d.Id, d.Name, d.Description, d.Module, d.IsActive);

    private WorkflowVersionDto MapVersion(WorkflowVersion v) =>
        new(
            v.Id,
            v.WorkflowDefinitionId,
            v.VersionNumber,
            v.Status,
            v.IsDefault,
            v.EffectiveFrom?.ToString("O"),
            v.EffectiveTo?.ToString("O"),
            v.CreatedOnUtc.ToString("O"),
            v.PublishedAtUtc?.ToString("O"),
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
            i.StartedAtUtc.ToString("O"),
            i.CompletedAtUtc?.ToString("O"),
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
