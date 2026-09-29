using ATSolution.SharedKernel.Constants;
using Configuration.Domain.Settings;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configuration.Infrastructure.Persistence.Configurations;

internal sealed class SystemSettingsRecordConfiguration : IEntityTypeConfiguration<SystemSettingsRecord>
{
    public void Configure(EntityTypeBuilder<SystemSettingsRecord> builder)
    {
        builder.ToTable(ConfigurationPersistenceConstants.SystemSettingsTableName, ConfigurationPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SettingsJson).HasColumnType("nvarchar(max)").IsRequired();
    }
}
