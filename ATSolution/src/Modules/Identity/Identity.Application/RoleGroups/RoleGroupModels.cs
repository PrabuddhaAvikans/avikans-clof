namespace Identity.Application.RoleGroups;

public sealed record RoleGroupDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> RoleNames,
    int UserCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateRoleGroupCommand(
    string Name,
    string Status,
    IReadOnlyList<Guid> RoleIds,
    string? Description = null);

public sealed record UpdateRoleGroupCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<Guid>? RoleIds = null,
    string? Status = null);
