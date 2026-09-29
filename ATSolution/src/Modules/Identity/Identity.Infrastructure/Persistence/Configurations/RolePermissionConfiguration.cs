using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.RolePermissionsTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(link => new { link.RoleId, link.PermissionId });

        builder.HasOne(link => link.Permission)
            .WithMany()
            .HasForeignKey(link => link.PermissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
