namespace Identity.Domain.Roles;

public class RoleGroupRole
{
    public Guid RoleGroupId { get; set; }
    public Guid RoleId { get; set; }
    public RoleGroup RoleGroup { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
