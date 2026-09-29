using Identity.Domain.Users;

namespace Identity.Domain.Roles;

public class UserRoleGroup
{
    public Guid UserId { get; set; }
    public Guid RoleGroupId { get; set; }
    public User User { get; set; } = null!;
    public RoleGroup RoleGroup { get; set; } = null!;
}
