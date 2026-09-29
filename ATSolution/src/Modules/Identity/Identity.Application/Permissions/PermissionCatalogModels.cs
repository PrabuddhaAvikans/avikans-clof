namespace Identity.Application.Permissions;

public sealed record PermissionDto(string Module, string Action, string Code);

public sealed record PermissionModuleGroupDto(
    string Module,
    IReadOnlyList<PermissionDto> Permissions);

public sealed record PermissionCatalogDto(
    IReadOnlyList<PermissionDto> Permissions,
    IReadOnlyList<PermissionModuleGroupDto> ByModule);
