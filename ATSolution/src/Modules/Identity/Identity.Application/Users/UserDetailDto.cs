namespace Identity.Application.Users;

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
