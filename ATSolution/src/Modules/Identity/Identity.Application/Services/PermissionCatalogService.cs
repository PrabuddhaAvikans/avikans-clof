using Identity.Application.Abstractions;
using Identity.Application.Permissions;

namespace Identity.Application.Services;

public sealed class PermissionCatalogService : IPermissionCatalogService
{
    public PermissionCatalogDto GetCatalog()
    {
        var permissions = IdentityPermissionCatalog.All
            .Select(p => new PermissionDto(p.Module, p.Action, p.Code))
            .ToList();

        return new PermissionCatalogDto(permissions, GroupByModule(permissions));
    }

    public IReadOnlyList<PermissionModuleGroupDto> GetByModule() =>
        GroupByModule(
            IdentityPermissionCatalog.All
                .Select(p => new PermissionDto(p.Module, p.Action, p.Code))
                .ToList());

    private static IReadOnlyList<PermissionModuleGroupDto> GroupByModule(
        IReadOnlyList<PermissionDto> permissions) =>
        permissions
            .GroupBy(p => p.Module, StringComparer.Ordinal)
            .OrderBy(g => g.Key)
            .Select(g => new PermissionModuleGroupDto(g.Key, g.ToList()))
            .ToList();
}
