using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Application.Abstractions;
using Identity.Application.Roles;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class RoleService : IRoleService
{
    private readonly IRepository<Role, Guid> _roles;
    private readonly IRepository<Permission, Guid> _permissions;
    private readonly IIdentityRepository _users;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public RoleService(
        IRepository<Role, Guid> roles,
        IRepository<Permission, Guid> permissions,
        IIdentityRepository users,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _roles = roles;
        _permissions = permissions;
        _users = users;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<RoleDto>> ListAsync(
        RoleListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var rolesQuery = _roles.Query()
            .AsNoTracking()
            .Include(role => role.RolePermissions)
            .ThenInclude(link => link.Permission)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            rolesQuery = rolesQuery.Where(role => role.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            rolesQuery = rolesQuery.Where(role =>
                role.Name.Contains(search)
                || (role.Description != null && role.Description.Contains(search)));
        }

        rolesQuery = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? rolesQuery.OrderByDescending(role => role.Name)
            : rolesQuery.OrderBy(role => role.Name);

        var totalCount = await rolesQuery.CountAsync(cancellationToken);
        var roles = await rolesQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var roleIds = roles.Select(role => role.Id).ToList();
        var userCounts = await _users.Query()
            .AsNoTracking()
            .Where(user => roleIds.Contains(user.RoleId) && user.Status == EntityStatuses.Active)
            .GroupBy(user => user.RoleId)
            .Select(group => new { RoleId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleId, item => item.Count, cancellationToken);

        var items = roles
            .Select(role => MapToDto(role, userCounts.GetValueOrDefault(role.Id)))
            .ToList();

        return PaginatedResponse<RoleDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<RoleDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await LoadRoleAsync(id, cancellationToken, asNoTracking: true);
        if (role is null)
        {
            return null;
        }

        var userCount = await CountActiveUsersAsync(id, cancellationToken);
        return MapToDto(role, userCount);
    }

    public async Task<RoleDto> CreateAsync(
        CreateRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var nameExists = await _roles.Query()
            .AnyAsync(role => role.Name == command.Name, cancellationToken);
        if (nameExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.Name),
                    string.Format(IdentityMessages.RoleNameAlreadyExists, command.Name),
                    ValidationErrorCodes.Conflict),
            ]);
        }

        var permissionIds = await ResolvePermissionIdsAsync(command.Permissions, cancellationToken);
        var role = Role.Create(command.Name, command.Description, command.Status);
        role.SetPermissions(permissionIds);

        await _roles.AddAsync(role, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await LoadRoleAsync(role.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, role.Id));

        return MapToDto(created, 0);
    }

    public async Task<RoleDto> UpdateAsync(
        UpdateRoleCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var role = await LoadRoleAsync(command.Id, cancellationToken, asNoTracking: false)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, command.Id));

        if (command.Name is not null
            && !string.Equals(command.Name, role.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _roles.Query()
                .AnyAsync(entity => entity.Name == command.Name && entity.Id != role.Id, cancellationToken);
            if (nameExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(
                        nameof(command.Name),
                        string.Format(IdentityMessages.RoleNameAlreadyExists, command.Name),
                        ValidationErrorCodes.Conflict),
                ]);
            }
        }

        if (role.IsSystem && command.Permissions is not null)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.Permissions),
                    IdentityMessages.SystemRoleCannotModifyPermissions,
                    ValidationErrorCodes.Conflict),
            ]);
        }

        role.Update(
            command.Name ?? role.Name,
            command.Description ?? role.Description,
            command.Status ?? role.Status);

        if (command.Permissions is not null && !role.IsSystem)
        {
            var permissionIds = await ResolvePermissionIdsAsync(command.Permissions, cancellationToken);
            role.SetPermissions(permissionIds);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await LoadRoleAsync(role.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, role.Id));
        var userCount = await CountActiveUsersAsync(role.Id, cancellationToken);

        return MapToDto(updated, userCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var role = await _roles.Query()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, id));

        if (role.IsSystem)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(id),
                    IdentityMessages.SystemRoleCannotDelete,
                    ValidationErrorCodes.Conflict),
            ]);
        }

        _roles.Remove(role);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<Guid>> ResolvePermissionIdsAsync(
        IReadOnlyList<string> permissionCodes,
        CancellationToken cancellationToken)
    {
        var distinctCodes = permissionCodes.Distinct(StringComparer.Ordinal).ToList();
        if (distinctCodes.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var permissions = await _permissions.Query()
            .AsNoTracking()
            .Where(permission => distinctCodes.Contains(permission.Code))
            .Select(permission => new { permission.Id, permission.Code })
            .ToListAsync(cancellationToken);

        if (permissions.Count != distinctCodes.Count)
        {
            throw new NotFoundException("One or more permissions were not found.");
        }

        return permissions.Select(permission => permission.Id).ToList();
    }

    private Task<int> CountActiveUsersAsync(Guid roleId, CancellationToken cancellationToken) =>
        _users.Query()
            .AsNoTracking()
            .CountAsync(
                user => user.RoleId == roleId && user.Status == EntityStatuses.Active,
                cancellationToken);

    private async Task<Role?> LoadRoleAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking)
    {
        var query = _roles.Query()
            .Include(role => role.RolePermissions)
            .ThenInclude(link => link.Permission)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

    private static RoleDto MapToDto(Role role, int userCount) =>
        new(
            role.Id,
            role.Name,
            role.Description,
            role.RolePermissions
                .Select(link => link.Permission.Code)
                .OrderBy(code => code, StringComparer.Ordinal)
                .ToList(),
            role.IsSystem,
            userCount,
            role.Status,
            role.CreatedOnUtc,
            role.ModifiedOnUtc);
}
