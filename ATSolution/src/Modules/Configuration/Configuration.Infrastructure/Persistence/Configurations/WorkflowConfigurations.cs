using ATSolution.SharedKernel.Constants;
using Configuration.Domain.Workflows;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Configuration.Infrastructure.Persistence.Configurations;

internal sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable(ConfigurationPersistenceConstants.WorkflowDefinitionsTableName, ConfigurationPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Module).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Name);
    }
}

internal sealed class WorkflowVersionConfiguration : IEntityTypeConfiguration<WorkflowVersion>
{
    public void Configure(EntityTypeBuilder<WorkflowVersion> builder)
    {
        builder.ToTable(ConfigurationPersistenceConstants.WorkflowVersionsTableName, ConfigurationPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StepsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.VersionNumber }).IsUnique();
    }
}

internal sealed class WorkflowRuleConfiguration : IEntityTypeConfiguration<WorkflowRule>
{
    public void Configure(EntityTypeBuilder<WorkflowRule> builder)
    {
        builder.ToTable(ConfigurationPersistenceConstants.WorkflowRulesTableName, ConfigurationPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ConditionsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.WorkflowDefinitionId);
    }
}

internal sealed class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable(ConfigurationPersistenceConstants.WorkflowInstancesTableName, ConfigurationPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SubjectType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SubjectId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkflowDefinitionName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StepsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.SubjectId);
        builder.HasIndex(x => x.Status);
    }
}
