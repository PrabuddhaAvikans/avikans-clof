using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.PermissionsTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(permission => permission.Id);

        builder.Property(permission => permission.Code)
            .HasMaxLength(UserFieldLengths.PermissionCode)
            .IsRequired();

        builder.Property(permission => permission.Module)
            .HasMaxLength(UserFieldLengths.PermissionModule)
            .IsRequired();

        builder.Property(permission => permission.Action)
            .HasMaxLength(UserFieldLengths.PermissionAction)
            .IsRequired();

        builder.HasIndex(permission => permission.Code)
            .IsUnique();
    }
}
