using ATSolution.SharedKernel.Models;

namespace Identity.Application.Roles;

public sealed class RoleListQuery : PaginatedRequest
{
    public string? Status { get; set; }
}
