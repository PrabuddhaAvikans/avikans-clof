using ATSolution.SharedKernel.Models;

namespace Identity.Application.Users;

public sealed class UserListQuery : PaginatedRequest
{
    public Guid? RoleId { get; set; }
    public Guid? RoleGroupId { get; set; }
    public string? Status { get; set; }
    public string? Department { get; set; }
}
