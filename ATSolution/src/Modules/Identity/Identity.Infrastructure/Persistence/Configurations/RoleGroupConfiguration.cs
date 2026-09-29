using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleGroupConfiguration : IEntityTypeConfiguration<RoleGroup>
{
    public void Configure(EntityTypeBuilder<RoleGroup> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.RoleGroupsTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Name)
            .HasMaxLength(UserFieldLengths.RoleName)
            .IsRequired();

        builder.Property(group => group.Description)
            .HasMaxLength(UserFieldLengths.Description);

        builder.Property(group => group.Status)
            .HasMaxLength(UserFieldLengths.Status)
            .IsRequired();

        builder.HasIndex(group => group.Name)
            .IsUnique();

        builder.HasMany(group => group.RoleGroupRoles)
            .WithOne(link => link.RoleGroup)
            .HasForeignKey(link => link.RoleGroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(group => group.UserRoleGroups)
            .WithOne(link => link.RoleGroup)
            .HasForeignKey(link => link.RoleGroupId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
