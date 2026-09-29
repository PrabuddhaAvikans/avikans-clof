using ATSolution.Domain.Entities.Common;
using Identity.Domain.Roles;

namespace Identity.Domain.Users;

public class User : Entity<Guid>, IAuditableEntity
{
    public string FirstName { get; private set; } = null!;
    public string LastName { get; private set; } = null!;
    public string FullName { get; set; } = null!;
    public string Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = null!;
    public string? Phone { get; private set; }
    public string? Department { get; private set; }
    public string? JobTitle { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public DateTimeOffset? LastLoginAtUtc { get; private set; }
    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;
    public ICollection<UserRoleGroup> UserRoleGroups { get; private set; } = new List<UserRoleGroup>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static User Create(
        string firstName,
        string lastName,
        string email,
        string passwordHash,
        Guid roleId,
        string? phone = null,
        string? department = null,
        string? jobTitle = null,
        string status = EntityStatuses.Active)
    {
        var now = DateTimeOffset.UtcNow;
        return new User
        {
            Id = Guid.NewGuid(),
            FirstName = firstName,
            LastName = lastName,
            FullName = BuildFullName(firstName, lastName),
            Email = email,
            PasswordHash = passwordHash,
            RoleId = roleId,
            Phone = phone,
            Department = department,
            JobTitle = jobTitle,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateProfile(
        string firstName,
        string lastName,
        string email,
        Guid roleId,
        string? phone,
        string? department,
        string? jobTitle,
        string status)
    {
        FirstName = firstName;
        LastName = lastName;
        FullName = BuildFullName(firstName, lastName);
        Email = email;
        RoleId = roleId;
        Phone = phone;
        Department = department;
        JobTitle = jobTitle;
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ApplyPartialUpdate(
        string? firstName,
        string? lastName,
        string? email,
        string? passwordHash)
    {
        if (firstName is not null)
        {
            FirstName = firstName;
        }

        if (lastName is not null)
        {
            LastName = lastName;
        }

        if (firstName is not null || lastName is not null)
        {
            FullName = BuildFullName(FirstName, LastName);
        }

        if (email is not null)
        {
            Email = email;
        }

        if (passwordHash is not null)
        {
            PasswordHash = passwordHash;
        }

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = LastLoginAtUtc.Value;
    }

    public void SetRoleGroups(IEnumerable<Guid> roleGroupIds)
    {
        UserRoleGroups.Clear();
        foreach (var roleGroupId in roleGroupIds.Distinct())
        {
            UserRoleGroups.Add(new UserRoleGroup
            {
                UserId = Id,
                RoleGroupId = roleGroupId,
            });
        }

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private static string BuildFullName(string firstName, string lastName) =>
        $"{firstName} {lastName}".Trim();
}
