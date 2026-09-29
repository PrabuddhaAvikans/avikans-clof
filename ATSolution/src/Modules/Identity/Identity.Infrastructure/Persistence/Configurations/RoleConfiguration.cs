using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.RolesTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Name)
            .HasMaxLength(UserFieldLengths.RoleName)
            .IsRequired();

        builder.Property(role => role.Description)
            .HasMaxLength(UserFieldLengths.Description);

        builder.Property(role => role.IsSystem)
            .IsRequired();

        builder.Property(role => role.Status)
            .HasMaxLength(UserFieldLengths.Status)
            .IsRequired();

        builder.HasIndex(role => role.Name)
            .IsUnique();

        builder.HasMany(role => role.RolePermissions)
            .WithOne(link => link.Role)
            .HasForeignKey(link => link.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
