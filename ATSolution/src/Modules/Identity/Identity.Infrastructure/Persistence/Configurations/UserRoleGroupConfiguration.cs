using ATSolution.SharedKernel.Constants;
using Identity.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Identity.Infrastructure.Persistence.Configurations;

internal sealed class UserRoleGroupConfiguration : IEntityTypeConfiguration<UserRoleGroup>
{
    public void Configure(EntityTypeBuilder<UserRoleGroup> builder)
    {
        builder.ToTable(IdentityPersistenceConstants.UserRoleGroupsTableName, IdentityPersistenceConstants.SchemaName);

        builder.HasKey(link => new { link.UserId, link.RoleGroupId });
    }
}
