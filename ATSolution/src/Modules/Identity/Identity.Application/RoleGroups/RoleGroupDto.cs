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
