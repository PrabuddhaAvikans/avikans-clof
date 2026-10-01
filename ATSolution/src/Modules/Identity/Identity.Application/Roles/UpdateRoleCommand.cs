using ATSolution.SharedKernel.Models;

namespace Identity.Application.Roles;

public sealed record UpdateRoleCommand(
    Guid Id,
    string? Name = null,
    string? Description = null,
    IReadOnlyList<string>? Permissions = null,
    string? Status = null);
