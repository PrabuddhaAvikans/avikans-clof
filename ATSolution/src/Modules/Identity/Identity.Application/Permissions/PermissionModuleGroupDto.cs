namespace Identity.Application.Permissions;

public sealed record PermissionModuleGroupDto(
    string Module,
    IReadOnlyList<PermissionDto> Permissions);
