using ATSolution.SharedKernel.Constants;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Sequences;
using Manufacturing.Domain.Tasks;
using Manufacturing.Domain.Work;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Manufacturing.Infrastructure.Persistence.Configurations;

internal sealed class ManufacturingJobConfiguration : IEntityTypeConfiguration<ManufacturingJob>
{
    public void Configure(EntityTypeBuilder<ManufacturingJob> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.JobsTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SalesOrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductVersionLabel).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.AssignedToName).HasMaxLength(300);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.OverallProgress).HasPrecision(18, 4);
        builder.Property(x => x.EstimatedCost).HasPrecision(18, 4);
        builder.Property(x => x.ActualCost).HasPrecision(18, 4);
        builder.Property(x => x.MaterialRequirementsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.QualityInspectionJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CompletionOutcomeJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ReworksJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Tasks).WithOne(x => x.Job).HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ManufacturingTaskConfiguration : IEntityTypeConfiguration<ManufacturingTask>
{
    public void Configure(EntityTypeBuilder<ManufacturingTask> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.TasksTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TaskNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.MachineName).HasMaxLength(200);
        builder.Property(x => x.AssignedToName).HasMaxLength(300);
        builder.Property(x => x.OperationSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PrerequisiteTaskIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.EstimatedHours).HasPrecision(18, 4);
        builder.Property(x => x.ActualHours).HasPrecision(18, 4);
        builder.Property(x => x.LabourCostRate).HasPrecision(18, 4);
        builder.Property(x => x.MachineCost).HasPrecision(18, 4);
        builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ReworkQuantity).HasPrecision(18, 4);
        builder.Property(x => x.WasteQuantity).HasPrecision(18, 4);
        builder.HasIndex(x => x.JobId);
        builder.HasMany(x => x.Units).WithOne(x => x.Task).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.History).WithOne(x => x.Task).HasForeignKey(x => x.TaskId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskUnitConfiguration : IEntityTypeConfiguration<TaskUnit>
{
    public void Configure(EntityTypeBuilder<TaskUnit> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.TaskUnitsTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProgressPercentage).HasPrecision(18, 4);
        builder.HasIndex(x => x.TaskId);
        builder.HasMany(x => x.Assignments).WithOne(x => x.TaskUnit).HasForeignKey(x => x.TaskUnitId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class TaskUnitAssignmentConfiguration : IEntityTypeConfiguration<TaskUnitAssignment>
{
    public void Configure(EntityTypeBuilder<TaskUnitAssignment> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.TaskUnitAssignmentsTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ContributionPercentage).HasPrecision(18, 4);
        builder.Property(x => x.ActualHours).HasPrecision(18, 4);
        builder.Property(x => x.NormalOvertimeHours).HasPrecision(18, 4);
        builder.Property(x => x.DoubleOvertimeHours).HasPrecision(18, 4);
        builder.Property(x => x.LaborCost).HasPrecision(18, 4);
        builder.Property(x => x.RejectedQuantity).HasPrecision(18, 4);
        builder.Property(x => x.WasteQuantity).HasPrecision(18, 4);
        builder.HasIndex(x => x.TaskUnitId);
    }
}

internal sealed class TaskHistoryEntryConfiguration : IEntityTypeConfiguration<TaskHistoryEntry>
{
    public void Configure(EntityTypeBuilder<TaskHistoryEntry> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.TaskHistoryEntriesTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OldStatus).HasMaxLength(50);
        builder.Property(x => x.NewStatus).HasMaxLength(50);
        builder.Property(x => x.Comments).HasMaxLength(4000);
        builder.HasIndex(x => x.TaskId);
    }
}

internal sealed class EmployeeWorkSessionConfiguration : IEntityTypeConfiguration<EmployeeWorkSession>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkSession> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.EmployeeWorkSessionsTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Remarks).HasMaxLength(2000);
        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.UserId);
    }
}

internal sealed class ManufacturingDocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable(ManufacturingPersistenceConstants.DocumentSequencesTableName, ManufacturingPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.DocumentType, x.Year }).IsUnique();
    }
}
