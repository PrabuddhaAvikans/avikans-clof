using ATSolution.SharedKernel.Models;

namespace Identity.Application.Roles;

public sealed class RoleListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}

public sealed record RoleDto(
    Guid Id,
    string Name,
    string? Description,
    IReadOnlyList<string> Permissions,
    bool IsSystem,
    int UserCount,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreateRoleCommand(
    string Name,
    string Status,
    IReadOnlyList<string> Permissions,
    string? Description = null);

public sealed record UpdateRoleCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<string>? Permissions = null,
    string? Status = null);
