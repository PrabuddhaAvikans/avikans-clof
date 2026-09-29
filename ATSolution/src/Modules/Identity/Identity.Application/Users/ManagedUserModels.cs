using ATSolution.SharedKernel.Models;

namespace Identity.Application.Users;

public sealed class UserListQuery : PaginatedRequest
{
    public Guid? RoleId { get; set; }
    public Guid? RoleGroupId { get; set; }
    public string? Status { get; set; }
    public string? Department { get; set; }
}

public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Phone,
    Guid RoleId,
    string RoleName,
    IReadOnlyList<Guid> RoleGroupIds,
    IReadOnlyList<string> RoleGroupNames,
    string? Department,
    string? JobTitle,
    string Status,
    DateTimeOffset? LastLoginAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record PermissionAssignmentDto(
    Guid UserId,
    Guid RoleId,
    IReadOnlyList<string> AdditionalPermissions,
    IReadOnlyList<string> RevokedPermissions,
    IReadOnlyList<string> EffectivePermissions);

public sealed record CreateManagedUserCommand(
    string Email,
    string FirstName,
    string LastName,
    Guid RoleId,
    IReadOnlyList<Guid> RoleGroupIds,
    string Status,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null,
    string? Password = null);

public sealed record UpdateManagedUserCommand(
    Guid Id,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    Guid? RoleId = null,
    IReadOnlyList<Guid>? RoleGroupIds = null,
    string? Status = null,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null);
