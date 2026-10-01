using ATSolution.SharedKernel.Models;

namespace Identity.Application.Users;

public sealed record UpdateManagedUserCommand(
    Guid Id,
    string? Email = null,
    string? FirstName = null,
    string? LastName = null,
    Guid? RoleId = null,
    IReadOnlyList<Guid>? RoleGroupIds = null,
    string? Status = null,
    string? Phone = null,
    string? Department = null,
    string? JobTitle = null);
