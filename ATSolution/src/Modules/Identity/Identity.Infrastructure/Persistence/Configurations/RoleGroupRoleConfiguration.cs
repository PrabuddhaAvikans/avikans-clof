using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class RoleGroupRoleConfiguration : IEntityTypeConfiguration<RoleGroupRole>
{
    public void Configure(EntityTypeBuilder<RoleGroupRole> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.RoleGroupRolesTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(link => new { link.RoleGroupId, link.RoleId });

        builder.HasOne(link => link.Role)
            .WithMany(role => role.RoleGroupRoles)
            .HasForeignKey(link => link.RoleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
