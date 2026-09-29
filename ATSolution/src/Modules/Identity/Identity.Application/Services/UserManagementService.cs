using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Abstractions.Validation;
using ATSolution.Application.Exceptions;
using ATSolution.SharedKernel.Constants;
using ATSolution.SharedKernel.Models;
using Identity.Application.Abstractions;
using Identity.Application.Users;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Services;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IIdentityRepository _users;
    private readonly IRepository<Role, Guid> _roles;
    private readonly IRepository<RoleGroup, Guid> _roleGroups;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPermissionResolver _permissionResolver;
    private readonly IApplicationValidator _validator;

    public UserManagementService(
        IIdentityRepository users,
        IRepository<Role, Guid> roles,
        IRepository<RoleGroup, Guid> roleGroups,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwordHasher,
        IPermissionResolver permissionResolver,
        IApplicationValidator validator)
    {
        _users = users;
        _roles = roles;
        _roleGroups = roleGroups;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
        _permissionResolver = permissionResolver;
        _validator = validator;
    }

    public async Task<PaginatedResponse<UserDetailDto>> ListAsync(
        UserListQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Max(1, query.PageSize);

        var usersQuery = _users.Query()
            .AsNoTracking()
            .Include(user => user.Role)
            .Include(user => user.UserRoleGroups)
            .ThenInclude(link => link.RoleGroup)
            .AsQueryable();

        if (query.RoleId is Guid roleId)
        {
            usersQuery = usersQuery.Where(user => user.RoleId == roleId);
        }

        if (query.RoleGroupId is Guid roleGroupId)
        {
            usersQuery = usersQuery.Where(user =>
                user.UserRoleGroups.Any(link => link.RoleGroupId == roleGroupId));
        }

        if (!string.IsNullOrWhiteSpace(query.Status))
        {
            usersQuery = usersQuery.Where(user => user.Status == query.Status);
        }

        if (!string.IsNullOrWhiteSpace(query.Department))
        {
            usersQuery = usersQuery.Where(user => user.Department == query.Department);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            usersQuery = usersQuery.Where(user =>
                user.FullName.Contains(search)
                || user.Email.Contains(search)
                || user.FirstName.Contains(search)
                || user.LastName.Contains(search)
                || (user.Department != null && user.Department.Contains(search))
                || (user.JobTitle != null && user.JobTitle.Contains(search)));
        }

        usersQuery = ApplySort(usersQuery, query.SortBy, query.SortDirection);

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var users = await usersQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return PaginatedResponse<UserDetailDto>.Create(
            users.Select(MapToDto).ToList(),
            totalCount,
            page,
            pageSize);
    }

    public async Task<UserDetailDto?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(id, cancellationToken, asNoTracking: true);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserDetailDto> CreateAsync(
        CreateManagedUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var emailExists = await _users.Query()
            .AnyAsync(user => user.Email == command.Email, cancellationToken);
        if (emailExists)
        {
            throw new ApplicationValidationException(
            [
                new ValidationError(
                    nameof(command.Email),
                    string.Format(IdentityMessages.UserEmailAlreadyExists, command.Email),
                    ValidationErrorCodes.Conflict),
            ]);
        }

        var roleExists = await _roles.Query()
            .AnyAsync(role => role.Id == command.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, command.RoleId));
        }

        await EnsureRoleGroupsExistAsync(command.RoleGroupIds, cancellationToken);

        var password = string.IsNullOrWhiteSpace(command.Password)
            ? IdentityMessages.DefaultPassword
            : command.Password;

        var user = User.Create(
            command.FirstName,
            command.LastName,
            command.Email,
            _passwordHasher.Hash(password),
            command.RoleId,
            command.Phone,
            command.Department,
            command.JobTitle,
            command.Status);

        user.SetRoleGroups(command.RoleGroupIds);

        await _users.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var created = await LoadUserAsync(user.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.UserNotFoundById, user.Id));

        return MapToDto(created);
    }

    public async Task<UserDetailDto> UpdateAsync(
        UpdateManagedUserCommand command,
        CancellationToken cancellationToken = default)
    {
        await _validator.ValidateAsync(command, cancellationToken);

        var user = await LoadUserAsync(command.Id, cancellationToken, asNoTracking: false)
            ?? throw new NotFoundException(string.Format(IdentityMessages.UserNotFoundById, command.Id));

        var email = command.Email ?? user.Email;
        if (!string.Equals(email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var emailExists = await _users.Query()
                .AnyAsync(entity => entity.Email == email && entity.Id != user.Id, cancellationToken);
            if (emailExists)
            {
                throw new ApplicationValidationException(
                [
                    new ValidationError(
                        nameof(command.Email),
                        string.Format(IdentityMessages.UserEmailAlreadyExists, email),
                        ValidationErrorCodes.Conflict),
                ]);
            }
        }

        var roleId = command.RoleId ?? user.RoleId;
        if (command.RoleId is not null)
        {
            var roleExists = await _roles.Query()
                .AnyAsync(role => role.Id == roleId, cancellationToken);
            if (!roleExists)
            {
                throw new NotFoundException(string.Format(IdentityMessages.RoleNotFound, roleId));
            }
        }

        if (command.RoleGroupIds is not null)
        {
            await EnsureRoleGroupsExistAsync(command.RoleGroupIds, cancellationToken);
        }

        user.UpdateProfile(
            command.FirstName ?? user.FirstName,
            command.LastName ?? user.LastName,
            email,
            roleId,
            command.Phone ?? user.Phone,
            command.Department ?? user.Department,
            command.JobTitle ?? user.JobTitle,
            command.Status ?? user.Status);

        if (command.RoleGroupIds is not null)
        {
            user.SetRoleGroups(command.RoleGroupIds);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var updated = await LoadUserAsync(user.Id, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.UserNotFoundById, user.Id));

        return MapToDto(updated);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await _users.Query()
            .FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken)
            ?? throw new NotFoundException(string.Format(IdentityMessages.UserNotFoundById, id));

        user.Deactivate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<PermissionAssignmentDto> GetPermissionAssignmentAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var user = await LoadUserAsync(userId, cancellationToken, asNoTracking: true)
            ?? throw new NotFoundException(string.Format(IdentityMessages.UserNotFoundById, userId));

        var permissions = await _permissionResolver.GetEffectivePermissionsAsync(user, cancellationToken);

        return new PermissionAssignmentDto(
            user.Id,
            user.RoleId,
            Array.Empty<string>(),
            Array.Empty<string>(),
            permissions);
    }

    private async Task EnsureRoleGroupsExistAsync(
        IReadOnlyList<Guid> roleGroupIds,
        CancellationToken cancellationToken)
    {
        if (roleGroupIds.Count == 0)
        {
            return;
        }

        var existingCount = await _roleGroups.Query()
            .CountAsync(group => roleGroupIds.Contains(group.Id), cancellationToken);

        if (existingCount != roleGroupIds.Distinct().Count())
        {
            throw new NotFoundException("One or more role groups were not found.");
        }
    }

    private async Task<User?> LoadUserAsync(
        Guid id,
        CancellationToken cancellationToken,
        bool asNoTracking)
    {
        var query = _users.Query()
            .Include(user => user.Role)
            .Include(user => user.UserRoleGroups)
            .ThenInclude(link => link.RoleGroup)
            .AsQueryable();

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    private static UserDetailDto MapToDto(User user)
    {
        var roleGroupIds = user.UserRoleGroups.Select(link => link.RoleGroupId).ToList();
        var roleGroupNames = user.UserRoleGroups
            .Select(link => link.RoleGroup?.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .ToList();

        return new UserDetailDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.Phone,
            user.RoleId,
            user.Role?.Name ?? string.Empty,
            roleGroupIds,
            roleGroupNames,
            user.Department,
            user.JobTitle,
            user.Status,
            user.LastLoginAtUtc,
            user.CreatedOnUtc,
            user.ModifiedOnUtc);
    }

    private static IQueryable<User> ApplySort(
        IQueryable<User> query,
        string? sortBy,
        string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant()) switch
        {
            "email" => descending ? query.OrderByDescending(user => user.Email) : query.OrderBy(user => user.Email),
            "firstname" => descending ? query.OrderByDescending(user => user.FirstName) : query.OrderBy(user => user.FirstName),
            "lastname" => descending ? query.OrderByDescending(user => user.LastName) : query.OrderBy(user => user.LastName),
            "status" => descending ? query.OrderByDescending(user => user.Status) : query.OrderBy(user => user.Status),
            "createdat" => descending ? query.OrderByDescending(user => user.CreatedOnUtc) : query.OrderBy(user => user.CreatedOnUtc),
            _ => descending
                ? query.OrderByDescending(user => user.FullName)
                : query.OrderBy(user => user.FullName),
        };
    }
}
