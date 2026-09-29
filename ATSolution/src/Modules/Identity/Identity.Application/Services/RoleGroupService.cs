using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Application.Abstractions;
using Identity.Application.RoleGroups;
using Identity.Application.Roles;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class RoleGroupService : IRoleGroupService
{
    private readonly IRepository<RoleGroup, Guid> _roleGroups;
    private readonly IRepository<Role, Guid> _roles;
    private readonly IIdentityRepository _users;
    private readonly IEntityRepository<UserRoleGroup> _userRoleGroups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationValidator _validator;

    public RoleGroupService(
        IRepository<RoleGroup, Guid> roleGroups,
        IRepository<Role, Guid> roles,
        IIdentityRepository users,
        IEntityRepository<UserRoleGroup> userRoleGroups,
        IUnitOfWork unitOfWork,
        IApplicationValidator validator)
    {
        _roleGroups = roleGroups;
        _roles = roles;
        _users = users;
        _userRoleGroups = userRoleGroups;
        _unitOfWork = unitOfWork;
        _validator = validator;
    }

    public async Task<PaginatedResponse<RoleGroupDto>> ListAsync(
        RoleListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var groupsQuery = _roleGroups.Query()
            .AsNoTracking()
            .Include(group => group.RoleGroupRoles)
            .ThenInclude(link => link.Role)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            groupsQuery = groupsQuery.Where(group => group.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            groupsQuery = groupsQuery.Where(group =>
                group.Name.Contains(search)
                || (group.Description != null && group.Description.Contains(search)));
        }

        groupsQuery = string.Equals(query.SortDirection, "desc", StringComparison.OrdinalIgnoreCase)
            ? groupsQuery.OrderByDescending(group => group.Name)
            : groupsQuery.OrderBy(group => group.Name);

        var totalCount = await groupsQuery.CountAsync(cancellationToken);
        var groups = await groupsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var groupIds = groups.Select(group => group.Id).ToList();
        var userCounts = await _userRoleGroups.Query()
            .AsNoTracking()
            .Where(link => groupIds.Contains(link.RoleGroupId))
            .Join(
                _users.Query().AsNoTracking()
                    .Where(user => user.Status == EntityStatuses.Active),
                link => link.UserId,
                user => user.Id,
                (link, _) => link.RoleGroupId)
            .GroupBy(roleGroupId => roleGroupId)
            .Select(group => new { RoleGroupId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.RoleGroupId, item => item.Count, cancellationToken);

        var items = groups
            .Select(group => MapToDto(group, userCounts.GetValueOrDefault(group.Id)))
            .ToList();

        return PaginatedResponse<RoleGroupDto>.Create(items, totalCount, page, pageSize);
    }

    public async Task<RoleGroupDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var group = await LoadGroupAsync(id, cancellationToken, asNoTracking: true);
        if (group is null)
        {
            return null;
        }

        var userCount = await CountActiveUsersAsync(id, cancellationToken);
        return MapToDto(group, userCount);
    }

    public async Task<RoleGroupDto> CreateAsync(
        CreateRoleGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var nameExists = await _roleGroups.Query()
            .AnyAsync(group => group.Name == command.Name, cancellationToken);
        if (nameExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.Name),
                    string.Format(IdentityMessages.RoleGroupNameAlreadyExists, command.Name),
                    ValidationErrorCodes.Conflict),
            ]);
        }

        await EnsureRolesExistAsync(command.RoleIds, cancellationToken);

        var group = RoleGroup.Create(command.Name, command.Description, command.Status);
        group.SetRoles(command.RoleIds);

        await _roleGroups.AddAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await LoadGroupAsync(group.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleGroupNotFound, group.Id));

        return MapToDto(created, 0);
    }

    public async Task<RoleGroupDto> UpdateAsync(
        UpdateRoleGroupCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var group = await LoadGroupAsync(command.Id, cancellationToken, asNoTracking: false)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleGroupNotFound, command.Id));

        if (command.Name is not null
            && !string.Equals(command.Name, group.Name, StringComparison.OrdinalIgnoreCase))
        {
            var nameExists = await _roleGroups.Query()
                .AnyAsync(entity => entity.Name == command.Name && entity.Id != group.Id, cancellationToken);
            if (nameExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(
                        nameof(command.Name),
                        string.Format(IdentityMessages.RoleGroupNameAlreadyExists, command.Name),
                        ValidationErrorCodes.Conflict),
                ]);
            }
        }

        if (command.RoleIds is not null)
        {
            await EnsureRolesExistAsync(command.RoleIds, cancellationToken);
        }

        group.Update(
            command.Name ?? group.Name,
            command.Description ?? group.Description,
            command.Status ?? group.Status);

        if (command.RoleIds is not null)
        {
            group.SetRoles(command.RoleIds);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await LoadGroupAsync(group.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleGroupNotFound, group.Id));
        var userCount = await CountActiveUsersAsync(group.Id, cancellationToken);

        return MapToDto(updated, userCount);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await _roleGroups.Query()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException(string.Format(IdentityMessages.RoleGroupNotFound, id));

        _roleGroups.Remove(group);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureRolesExistAsync(
        IReadOnlyList<Guid> roleIds,
        CancellationToken cancellationToken)
    {
        if (roleIds.Count == 0)
        {
            return;
        }

        var existingCount = await _roles.Query()
            .CountAsync(role => roleIds.Contains(role.Id), cancellationToken);

        if (existingCount != roleIds.Distinct().Count())
        {
            throw new NotFoundException("One or more roles were not found.");
        }
    }

    private Task<int> CountActiveUsersAsync(Guid roleGroupId, CancellationToken cancellationToken) =>
        _userRoleGroups.Query()
            .AsNoTracking()
            .Where(link => link.RoleGroupId == roleGroupId)
            .Join(
                _users.Query().AsNoTracking()
                    .Where(user => user.Status == EntityStatuses.Active),
                link => link.UserId,
                user => user.Id,
                (_, _) => 1)
            .CountAsync(cancellationToken);

    private async Task<RoleGroup?> LoadGroupAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking)
    {
        var query = _roleGroups.Query()
            .Include(group => group.RoleGroupRoles)
            .ThenInclude(link => link.Role)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(group => group.Id == id, cancellationToken);
    }

    private static RoleGroupDto MapToDto(RoleGroup group, int userCount)
    {
        var roleIds = group.RoleGroupRoles.Select(link => link.RoleId).ToList();
        var roleNames = group.RoleGroupRoles
            .Select(link => link.Role?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        return new RoleGroupDto(
            group.Id,
            group.Name,
            group.Description,
            roleIds,
            roleNames,
            userCount,
            group.Status,
            group.CreatedOnUtc,
            group.ModifiedOnUtc);
    }
}
