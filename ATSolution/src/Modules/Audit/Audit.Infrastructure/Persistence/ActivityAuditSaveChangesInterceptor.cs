using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application.Abstractions.Identity;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Domain.Entities.Common;
using ATSolution.Infrastructure.Persistence.Data;
using ATSolution.Infrastructure.Persistence.Repositories;
using Audit.Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Audit.Infrastructure.Persistence;

/// <summary>
/// Writes activity audit rows for mapped domain entity create/update/delete operations.
/// Skips unauthenticated requests (e.g. seeding) and the AuditLogEntry entity itself.
/// </summary>
public sealed class ActivityAuditSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly HashSet<string> IgnoredProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(IAuditableEntity.CreatedOnUtc),
        nameof(IAuditableEntity.ModifiedOnUtc),
        "PasswordHash",
        "Id",
    };

    private static readonly Dictionary<string, string> EntityNameByType = new(StringComparer.Ordinal)
    {
        ["User"] = "User",
        ["Role"] = "Role",
        ["RoleGroup"] = "RoleGroup",
        ["Quotation"] = "Quotation",
        ["SalesOrder"] = "SalesOrder",
        ["Product"] = "Product",
        ["Customer"] = "Customer",
        ["InventoryItem"] = "InventoryItem",
        ["ManufacturingJob"] = "ManufacturingJob",
        ["Delivery"] = "Delivery",
        ["SystemSettingsRecord"] = "Settings",
    };

    private readonly ICurrentUser _currentUser;

    public ActivityAuditSaveChangesInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        AppendAuditEntriesAsync(eventData.Context, CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return base.SavingChanges(eventData, result);
    }

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        await AppendAuditEntriesAsync(eventData.Context, cancellationToken);
        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private async Task AppendAuditEntriesAsync(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is not SqlDbContext sql || !_currentUser.IsAuthenticated)
            return;

        var tracked = context.ChangeTracker
            .Entries()
            .Where(e => e.Entity is not AuditLogEntry)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Where(e => TryMapEntityName(e.Entity.GetType(), out _))
            .ToList();

        if (tracked.Count == 0)
            return;

        // Attach to the context already inside this SaveChanges, via the core repository.
        IRepository<AuditLogEntry, Guid> logs = new Repository<AuditLogEntry, Guid>(sql);
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in tracked)
        {
            TryMapEntityName(entry.Entity.GetType(), out var entityName);

            var action = entry.State switch
            {
                EntityState.Added => "created",
                EntityState.Deleted => "deleted",
                _ => "updated",
            };

            var changes = BuildChanges(entry);
            if (entry.State == EntityState.Modified && changes.Count == 0)
                continue;

            var entityId = ResolveEntityId(entry);
            var entityLabel = ResolveEntityLabel(entry);
            var details = BuildDetails(action, entityName, entityLabel, entityId);
            var severity = action == "deleted" ? AuditSeverities.Warning : AuditSeverities.Info;
            var changesJson = changes.Count == 0
                ? null
                : JsonSerializer.Serialize(changes, JsonOptions);

            await logs.AddAsync(
                AuditLogEntry.Create(
                    now,
                    _currentUser.UserId,
                    _currentUser.UserName,
                    action,
                    entityName,
                    entityId,
                    entityLabel,
                    details,
                    severity,
                    _currentUser.IpAddress,
                    _currentUser.UserAgent,
                    changesJson),
                cancellationToken);
        }
    }

    private static bool TryMapEntityName(Type type, out string entityName)
    {
        // Prefer CLR type name so namespace collisions (e.g. Delivery) still map.
        if (EntityNameByType.TryGetValue(type.Name, out entityName!))
            return true;

        entityName = string.Empty;
        return false;
    }

    private static List<object> BuildChanges(EntityEntry entry)
    {
        var changes = new List<object>();

        if (entry.State == EntityState.Deleted)
        {
            foreach (var property in entry.Properties)
            {
                if (ShouldSkipProperty(property))
                    continue;

                var from = IsOpaqueProperty(property.Metadata.Name)
                    ? "[previous]"
                    : FormatValue(property.OriginalValue);
                if (from is null)
                    continue;

                changes.Add(new { field = ToCamelCase(property.Metadata.Name), from });
            }

            return changes;
        }

        foreach (var property in entry.Properties)
        {
            if (ShouldSkipProperty(property))
                continue;

            if (entry.State == EntityState.Modified && !property.IsModified)
                continue;

            string? from;
            string? to;

            if (IsOpaqueProperty(property.Metadata.Name))
            {
                from = entry.State == EntityState.Added ? null : "[previous]";
                to = entry.State == EntityState.Deleted ? null : "[updated]";
            }
            else
            {
                from = entry.State == EntityState.Added
                    ? null
                    : FormatValue(property.OriginalValue);
                to = entry.State == EntityState.Deleted
                    ? null
                    : FormatValue(property.CurrentValue);

                if (entry.State == EntityState.Modified
                    && string.Equals(from, to, StringComparison.Ordinal))
                    continue;
            }

            if (from is null && to is null)
                continue;

            changes.Add(new { field = ToCamelCase(property.Metadata.Name), from, to });
        }

        return changes;
    }

    private static bool ShouldSkipProperty(PropertyEntry property)
    {
        var name = property.Metadata.Name;
        if (IgnoredProperties.Contains(name))
            return true;

        if (property.Metadata.IsPrimaryKey())
            return true;

        if (property.Metadata.IsShadowProperty())
            return true;

        return false;
    }

    private static bool IsOpaqueProperty(string name) =>
        name.EndsWith("Json", StringComparison.OrdinalIgnoreCase)
        || name.Contains("Password", StringComparison.OrdinalIgnoreCase);

    private static string? FormatValue(object? value) =>
        value switch
        {
            null => null,
            DateTimeOffset dto => dto.ToString("O"),
            DateTime dt => dt.ToUniversalTime().ToString("O"),
            bool b => b ? "true" : "false",
            _ => Truncate(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)),
        };

    private static string? Truncate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        const int max = 500;
        return value.Length <= max ? value : value[..max] + "…";
    }

    private static string ResolveEntityId(EntityEntry entry)
    {
        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        var value = entry.State == EntityState.Deleted
            ? idProp?.OriginalValue
            : idProp?.CurrentValue;

        return value?.ToString() ?? "unknown";
    }

    private static string? ResolveEntityLabel(EntityEntry entry)
    {
        string[] candidates =
        [
            "FullName",
            "Name",
            "Number",
            "Code",
            "Sku",
            "JobNumber",
            "Title",
            "Email",
        ];

        foreach (var candidate in candidates)
        {
            var prop = entry.Properties.FirstOrDefault(p =>
                string.Equals(p.Metadata.Name, candidate, StringComparison.OrdinalIgnoreCase));
            if (prop is null)
                continue;

            var value = entry.State == EntityState.Deleted ? prop.OriginalValue : prop.CurrentValue;
            var text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(text))
                return Truncate(text);
        }

        return null;
    }

    private static string BuildDetails(string action, string entityName, string? entityLabel, string entityId)
    {
        var subject = string.IsNullOrWhiteSpace(entityLabel) ? entityId : entityLabel;
        return action switch
        {
            "created" => $"Created {entityName} {subject}",
            "deleted" => $"Deleted {entityName} {subject}",
            _ => $"Updated {entityName} {subject}",
        };
    }

    private static string ToCamelCase(string name)
    {
        if (string.IsNullOrEmpty(name) || char.IsLower(name[0]))
            return name;

        return char.ToLowerInvariant(name[0]) + name[1..];
    }
}
