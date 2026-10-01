namespace Identity.Application.Permissions;

public sealed record PermissionCatalogDto(
    IReadOnlyList<PermissionDto> Permissions,
    IReadOnlyList<PermissionModuleGroupDto> ByModule);
