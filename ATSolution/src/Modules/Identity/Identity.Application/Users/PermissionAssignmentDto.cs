namespace Identity.Application.Users;

public sealed record PermissionAssignmentDto(
    Guid UserId,
    Guid RoleId,
    IReadOnlyList<string> AdditionalPermissions,
    IReadOnlyList<string> RevokedPermissions,
    IReadOnlyList<string> EffectivePermissions);
