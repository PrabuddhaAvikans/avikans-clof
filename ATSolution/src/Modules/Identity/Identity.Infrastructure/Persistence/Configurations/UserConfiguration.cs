using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.UsersTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(user => user.Id);

        builder.Property(user => user.FirstName)
            .HasMaxLength(UserFieldLengths.FirstName)
            .IsRequired();

        builder.Property(user => user.LastName)
            .HasMaxLength(UserFieldLengths.LastName)
            .IsRequired();

        builder.Property(user => user.FullName)
            .HasMaxLength(UserFieldLengths.FullName)
            .IsRequired();

        builder.Property(user => user.Email)
            .HasMaxLength(UserFieldLengths.Email)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(UserFieldLengths.PasswordHash)
            .IsRequired();

        builder.Property(user => user.Phone)
            .HasMaxLength(UserFieldLengths.Phone);

        builder.Property(user => user.Department)
            .HasMaxLength(UserFieldLengths.Department);

        builder.Property(user => user.JobTitle)
            .HasMaxLength(UserFieldLengths.JobTitle);

        builder.Property(user => user.Status)
            .HasMaxLength(UserFieldLengths.Status)
            .IsRequired();

        builder.Property(user => user.LastLoginAtUtc);

        builder.Property(user => user.RoleId)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();

        builder.HasOne(user => user.Role)
            .WithMany(role => role.Users)
            .HasForeignKey(user => user.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(user => user.UserRoleGroups)
            .WithOne(link => link.User)
            .HasForeignKey(link => link.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(user => user.UserRoleGroups)
            .AutoInclude(false);
    }
}
