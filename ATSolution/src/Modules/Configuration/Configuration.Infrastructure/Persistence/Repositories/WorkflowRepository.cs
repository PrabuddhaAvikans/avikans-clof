using Configuration.Application.Abstractions;
using Configuration.Domain.Workflows;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using SqlDbContext = ATSolution.Infrastructure.Persistence.Data.SqlDbContext;

namespace Configuration.Infrastructure.Persistence.Repositories;

internal sealed class WorkflowRepository : IWorkflowRepository
{
    private readonly SqlDbContext _context;

    public WorkflowRepository(SqlDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowDefinition>()
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersion>> ListVersionsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowVersion>()
            .AsNoTracking()
            .OrderBy(v => v.VersionNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowRule>> ListRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowRule>()
            .AsNoTracking()
            .OrderBy(r => r.Priority)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkflowDefinition?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowDefinition>()
            .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);
    }

    public async Task AddDefinitionAsync(WorkflowDefinition definition, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowDefinition>().AddAsync(definition, cancellationToken);
    }

    public async Task<int> GetMaxVersionNumberAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowVersion>()
            .Where(v => v.WorkflowDefinitionId == definitionId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(cancellationToken) ?? 0;
    }

    public async Task AddVersionAsync(WorkflowVersion version, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowVersion>().AddAsync(version, cancellationToken);
    }

    public Task<WorkflowVersion?> GetVersionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowVersion>()
            .FirstOrDefaultAsync(v => v.Id == id, cancellationToken);
    }

    public Task<WorkflowVersion?> FindDraftVersionAsync(Guid definitionId, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowVersion>()
            .FirstOrDefaultAsync(
                v => v.WorkflowDefinitionId == definitionId
                    && v.Status == WorkflowVersionStatuses.Draft,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersion>> ListSiblingVersionsAsync(
        Guid definitionId,
        Guid excludeVersionId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowVersion>()
            .Where(v => v.WorkflowDefinitionId == definitionId && v.Id != excludeVersionId)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkflowRule?> GetRuleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowRule>()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task AddRuleAsync(WorkflowRule rule, CancellationToken cancellationToken = default)
    {
        await _context.Set<WorkflowRule>().AddAsync(rule, cancellationToken);
    }

    public Task<bool> AnyDefinitionForModuleAsync(string module, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowDefinition>()
            .AnyAsync(d => d.Module == module, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowDefinition>> ListDefinitionsByModuleAsync(
        string module,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowDefinition>()
            .Where(d => d.Module == module)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowVersion>> ListVersionsByDefinitionIdsAsync(
        IReadOnlyCollection<Guid> definitionIds,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowVersion>()
            .Where(v => definitionIds.Contains(v.WorkflowDefinitionId))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WorkflowRule>> ListRulesByDefinitionIdsAsync(
        IReadOnlyCollection<Guid> definitionIds,
        CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowRule>()
            .Where(r => definitionIds.Contains(r.WorkflowDefinitionId))
            .ToListAsync(cancellationToken);
    }

    public void RemoveRules(IEnumerable<WorkflowRule> rules)
    {
        _context.Set<WorkflowRule>().RemoveRange(rules);
    }

    public void RemoveVersions(IEnumerable<WorkflowVersion> versions)
    {
        _context.Set<WorkflowVersion>().RemoveRange(versions);
    }

    public void RemoveDefinitions(IEnumerable<WorkflowDefinition> definitions)
    {
        _context.Set<WorkflowDefinition>().RemoveRange(definitions);
    }

    public async Task<IReadOnlyList<WorkflowInstance>> ListInstancesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<WorkflowInstance>()
            .AsNoTracking()
            .OrderByDescending(i => i.StartedAtUtc)
            .ToListAsync(cancellationToken);
    }

    public Task<WorkflowInstance?> GetInstanceAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return _context.Set<WorkflowInstance>()
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, Guid>> FindRoleIdsByNamesAsync(
        IEnumerable<string> names,
        CancellationToken cancellationToken = default)
    {
        var wanted = names
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (wanted.Count == 0)
        {
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }

        var roles = await _context.Set<Role>()
            .AsNoTracking()
            .Where(role => wanted.Contains(role.Name))
            .Select(role => new { role.Id, role.Name })
            .ToListAsync(cancellationToken);

        return roles.ToDictionary(
            role => role.Name,
            role => role.Id,
            StringComparer.OrdinalIgnoreCase);
    }
}
