using ATSolution.SharedKernel.Constants;
using Audit.Domain.AuditLogs;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Audit.Infrastructure.Persistence.Configurations;

internal sealed class AuditLogEntryConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable(AuditPersistenceConstants.AuditLogsTableName, AuditPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Entity).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityLabel).HasMaxLength(500);
        builder.Property(x => x.Details).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Severity).HasMaxLength(50).IsRequired();
        builder.Property(x => x.IpAddress).HasMaxLength(100);
        builder.Property(x => x.UserAgent).HasMaxLength(1000);
        builder.Property(x => x.ChangesJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => x.Timestamp);
        builder.HasIndex(x => x.Entity);
        builder.HasIndex(x => x.UserId);
        builder.HasIndex(x => x.Severity);
    }
}
