using ATSolution.SharedKernel.Models;

namespace Identity.Application.Users;

public sealed record CreateManagedUserCommand(
    string Email,
    string FirstName,
    string LastName,
    Guid RoleId,
    IReadOnlyList<Guid> RoleGroupIds,
    string Status,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null,
    string? Password = null);
