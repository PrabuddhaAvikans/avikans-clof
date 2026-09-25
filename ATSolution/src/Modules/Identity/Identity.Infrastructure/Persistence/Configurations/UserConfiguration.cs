using ATSolution.SharedKernel.Constants;
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

        builder.Property(user => user.Email)
            .HasMaxLength(UserFieldLengths.Email)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(UserFieldLengths.PasswordHash)
            .IsRequired();

        builder.HasIndex(user => user.Email)
            .IsUnique();
    }
}