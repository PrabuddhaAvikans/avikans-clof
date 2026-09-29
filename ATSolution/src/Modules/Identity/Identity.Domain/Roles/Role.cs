using ATSolution.Domain.Entities.Common;
using Identity.Domain.Users;

namespace Identity.Domain.Roles;

public class Role : Entity<Guid>, IAuditableEntity
{
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<User> Users { get; private set; } = new List<User>();
    public ICollection<RoleGroupRole> RoleGroupRoles { get; private set; } = new List<RoleGroupRole>();

    public static Role Create(
        string name,
        string? description,
        string status = EntityStatuses.Active,
        bool isSystem = false)
    {
        var now = DateTimeOffset.UtcNow;
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            IsSystem = isSystem,
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

    public void SetPermissions(IEnumerable<Guid> permissionIds)
    {
        RolePermissions.Clear();
        foreach (var permissionId in permissionIds.Distinct())
        {
            RolePermissions.Add(new RolePermission
            {
                RoleId = Id,
                PermissionId = permissionId,
            });
        }

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
