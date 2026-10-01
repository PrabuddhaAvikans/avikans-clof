namespace Identity.Application.RoleGroups;

public sealed record UpdateRoleGroupCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<Guid>? RoleIds = null,
    string? Status = null);
