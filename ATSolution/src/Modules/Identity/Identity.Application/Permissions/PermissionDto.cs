namespace Identity.Application.Permissions;

public sealed record PermissionDto(string Module, string Action, string Code);
