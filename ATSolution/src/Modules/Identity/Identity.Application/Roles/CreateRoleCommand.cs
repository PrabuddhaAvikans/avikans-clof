using ATSolution.SharedKernel.Models;

namespace Identity.Application.Roles;

public sealed record CreateRoleCommand(
    string Name,
    string Status,
    IReadOnlyList<string> Permissions,
    string? Description = null);
