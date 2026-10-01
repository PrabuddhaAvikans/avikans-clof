namespace Identity.Application.RoleGroups;

public sealed record CreateRoleGroupCommand(
    string Name,
    string Status,
    IReadOnlyList<Guid> RoleIds,
    string? Description = null);
