using ATSolution.Domain.Entities.Common;

namespace Identity.Domain.Roles;

public class RoleGroup : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }
    public ICollection<RoleGroupRole> RoleGroupRoles { get; private set; } = new List<RoleGroupRole>();
    public ICollection<UserRoleGroup> UserRoleGroups { get; private set; } = new List<UserRoleGroup>();

    public static RoleGroup Create(
        string name,
        string? description,
        string status = EntityStatuses.Active)
    {
        var now = DateTimeOffset.UtcNow;
        return new RoleGroup
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(string name, string? description, string status)
    {
        Name = name;
        Description = description;
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetRoles(IEnumerable<Guid> roleIds)
    {
        RoleGroupRoles.Clear();
        foreach (var roleId in roleIds.Distinct())
        {
            RoleGroupRoles.Add(new RoleGroupRole
            {
                RoleGroupId = Id,
                RoleId = roleId,
            });
        }

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
