using ATSolution.Application.Abstractions.Persistence;
using Identity.Application.Abstractions;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class PermissionResolver : IPermissionResolver
{
    private readonly IEntityRepository<RolePermission> _rolePermissions;
    private readonly IEntityRepository<RoleGroupRole> _roleGroupRoles;
    private readonly IEntityRepository<UserRoleGroup> _userRoleGroups;
    private readonly IRepository<Role, Guid> _roles;
    private readonly IRepository<RoleGroup, Guid> _roleGroups;
    private readonly IRepository<Permission, Guid> _permissions;

    public PermissionResolver(
        IEntityRepository<RolePermission> rolePermissions,
        IEntityRepository<RoleGroupRole> roleGroupRoles,
        IEntityRepository<UserRoleGroup> userRoleGroups,
        IRepository<Role, Guid> roles,
        IRepository<RoleGroup, Guid> roleGroups,
        IRepository<Permission, Guid> permissions)
    {
        _rolePermissions = rolePermissions;
        _roleGroupRoles = roleGroupRoles;
        _userRoleGroups = userRoleGroups;
        _roles = roles;
        _roleGroups = roleGroups;
        _permissions = permissions;
    }

    public async Task<IReadOnlyList<string>> GetEffectivePermissionsAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        var permissions = new HashSet<string>(StringComparer.Ordinal);

        var primaryRolePermissions = await _rolePermissions.Query()
            .AsNoTracking()
            .Where(link => link.RoleId == user.RoleId)
            .Join(
                _roles.Query().AsNoTracking().Where(role => role.Status == EntityStatuses.Active),
                link => link.RoleId,
                role => role.Id,
                (link, _) => link)
            .Join(
                _permissions.Query().AsNoTracking(),
                link => link.PermissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .ToListAsync(cancellationToken);

        foreach (var code in primaryRolePermissions)
        {
            permissions.Add(code);
        }

        var roleGroupIds = user.UserRoleGroups.Count > 0
            ? user.UserRoleGroups.Select(link => link.RoleGroupId).ToList()
            : await _userRoleGroups.Query()
                .AsNoTracking()
                .Where(link => link.UserId == user.Id)
                .Select(link => link.RoleGroupId)
                .ToListAsync(cancellationToken);

        if (roleGroupIds.Count == 0)
        {
            return permissions.OrderBy(code => code, StringComparer.Ordinal).ToList();
        }

        var groupRoleIds = await _roleGroupRoles.Query()
            .AsNoTracking()
            .Where(link => roleGroupIds.Contains(link.RoleGroupId))
            .Join(
                _roleGroups.Query().AsNoTracking().Where(group => group.Status == EntityStatuses.Active),
                link => link.RoleGroupId,
                group => group.Id,
                (link, _) => link.RoleId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (groupRoleIds.Count == 0)
        {
            return permissions.OrderBy(code => code, StringComparer.Ordinal).ToList();
        }

        var groupPermissions = await _rolePermissions.Query()
            .AsNoTracking()
            .Where(link => groupRoleIds.Contains(link.RoleId))
            .Join(
                _roles.Query().AsNoTracking().Where(role => role.Status == EntityStatuses.Active),
                link => link.RoleId,
                role => role.Id,
                (link, _) => link)
            .Join(
                _permissions.Query().AsNoTracking(),
                link => link.PermissionId,
                permission => permission.Id,
                (_, permission) => permission.Code)
            .ToListAsync(cancellationToken);

        foreach (var code in groupPermissions)
        {
            permissions.Add(code);
        }

        return permissions.OrderBy(code => code, StringComparer.Ordinal).ToList();
    }
}
