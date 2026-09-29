using ATSolution.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PeriodClose.Domain.Audit;
using PeriodClose.Domain.Periods;
using PeriodClose.Domain.Settings;
using PeriodClose.Domain.Snapshots;
using PeriodClose.Domain.Summaries;
using PeriodClose.Domain.Validations;

namespace PeriodClose.Infrastructure.Persistence.Configurations;

internal sealed class BusinessPeriodConfiguration : IEntityTypeConfiguration<BusinessPeriod>
{
    public void Configure(EntityTypeBuilder<BusinessPeriod> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.BusinessPeriodsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OpenedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OpenedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClosedBy).HasMaxLength(100);
        builder.Property(x => x.ClosedByName).HasMaxLength(200);
        builder.Property(x => x.ReopenedBy).HasMaxLength(100);
        builder.Property(x => x.ReopenedByName).HasMaxLength(200);
        builder.Property(x => x.ReopenReason).HasMaxLength(2000);
        builder.Property(x => x.OriginalClosedBy).HasMaxLength(100);
        builder.Property(x => x.OriginalClosedByName).HasMaxLength(200);
        builder.HasIndex(x => new { x.BranchId, x.BusinessDate }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

internal sealed class MonthlyPeriodConfiguration : IEntityTypeConfiguration<MonthlyPeriod>
{
    public void Configure(EntityTypeBuilder<MonthlyPeriod> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.MonthlyPeriodsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StartedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.StartedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ClosedBy).HasMaxLength(100);
        builder.Property(x => x.ClosedByName).HasMaxLength(200);
        builder.Property(x => x.ReopenedBy).HasMaxLength(100);
        builder.Property(x => x.ReopenedByName).HasMaxLength(200);
        builder.Property(x => x.ReopenReason).HasMaxLength(2000);
        builder.Property(x => x.OriginalClosedBy).HasMaxLength(100);
        builder.Property(x => x.OriginalClosedByName).HasMaxLength(200);
        builder.HasIndex(x => new { x.BranchId, x.Year, x.Month }).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

internal sealed class PeriodCloseSettingsConfiguration : IEntityTypeConfiguration<PeriodCloseSettings>
{
    public void Configure(EntityTypeBuilder<PeriodCloseSettings> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.PeriodCloseSettingsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkerSessionCloseRule).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BranchId).IsUnique();
    }
}

internal sealed class DayCloseValidationIssueConfiguration : IEntityTypeConfiguration<DayCloseValidationIssue>
{
    public void Configure(EntityTypeBuilder<DayCloseValidationIssue> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.DayValidationsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValidationCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ValidationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.HasIndex(x => x.BusinessPeriodId);
    }
}

internal sealed class MonthlyCloseValidationIssueConfiguration : IEntityTypeConfiguration<MonthlyCloseValidationIssue>
{
    public void Configure(EntityTypeBuilder<MonthlyCloseValidationIssue> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.MonthValidationsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ValidationCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ValidationType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100);
        builder.Property(x => x.EntityId).HasMaxLength(100);
        builder.HasIndex(x => x.MonthlyPeriodId);
    }
}

internal sealed class DailyClosingSummaryConfiguration : IEntityTypeConfiguration<DailyClosingSummary>
{
    public void Configure(EntityTypeBuilder<DailyClosingSummary> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.DailySummariesTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CompletedProductionQty).HasPrecision(18, 4);
        builder.Property(x => x.PartialProductionQty).HasPrecision(18, 4);
        builder.Property(x => x.InvoiceTotal).HasPrecision(18, 4);
        builder.Property(x => x.PaymentTotal).HasPrecision(18, 4);
        builder.Property(x => x.QuotationValue).HasPrecision(18, 4);
        builder.Property(x => x.SalesOrderValue).HasPrecision(18, 4);
        builder.Property(x => x.CreditNoteTotal).HasPrecision(18, 4);
        builder.Property(x => x.CashPayments).HasPrecision(18, 4);
        builder.Property(x => x.CardPayments).HasPrecision(18, 4);
        builder.Property(x => x.BankPayments).HasPrecision(18, 4);
        builder.Property(x => x.AdvancePayments).HasPrecision(18, 4);
        builder.Property(x => x.Refunds).HasPrecision(18, 4);
        builder.Property(x => x.OpeningReceivable).HasPrecision(18, 4);
        builder.Property(x => x.ClosingReceivable).HasPrecision(18, 4);
        builder.Property(x => x.OutstandingAmount).HasPrecision(18, 4);
        builder.Property(x => x.TransactionRefsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.BusinessPeriodId).IsUnique();
    }
}

internal sealed class MonthlyClosingSummaryConfiguration : IEntityTypeConfiguration<MonthlyClosingSummary>
{
    public void Configure(EntityTypeBuilder<MonthlyClosingSummary> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.MonthlySummariesTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.SalesTotal).HasPrecision(18, 4);
        builder.Property(x => x.PurchaseTotal).HasPrecision(18, 4);
        builder.Property(x => x.PaymentTotal).HasPrecision(18, 4);
        builder.Property(x => x.ExpenseTotal).HasPrecision(18, 4);
        builder.Property(x => x.InventoryValue).HasPrecision(18, 4);
        builder.Property(x => x.WipValue).HasPrecision(18, 4);
        builder.Property(x => x.CostOfGoodsSold).HasPrecision(18, 4);
        builder.Property(x => x.GrossProfit).HasPrecision(18, 4);
        builder.Property(x => x.RawMaterials).HasPrecision(18, 4);
        builder.Property(x => x.Labour).HasPrecision(18, 4);
        builder.Property(x => x.Production).HasPrecision(18, 4);
        builder.Property(x => x.Waste).HasPrecision(18, 4);
        builder.Property(x => x.ReusableWaste).HasPrecision(18, 4);
        builder.Property(x => x.Overhead).HasPrecision(18, 4);
        builder.Property(x => x.CreditNotes).HasPrecision(18, 4);
        builder.Property(x => x.NetMargin).HasPrecision(18, 4);
        builder.Property(x => x.TransactionRefsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.MonthlyPeriodId).IsUnique();
    }
}

internal sealed class ProductionDailySnapshotConfiguration : IEntityTypeConfiguration<ProductionDailySnapshot>
{
    public void Configure(EntityTypeBuilder<ProductionDailySnapshot> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.ProductionDailySnapshotsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.ProductionOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductionOrderNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.WorkerId).HasMaxLength(100);
        builder.Property(x => x.WorkerName).HasMaxLength(200);
        builder.Property(x => x.JobStatus).HasMaxLength(50);
        builder.Property(x => x.TaskStatus).HasMaxLength(50);
        builder.Property(x => x.TotalQty).HasPrecision(18, 4);
        builder.Property(x => x.CompletedQty).HasPrecision(18, 4);
        builder.Property(x => x.PartialQty).HasPrecision(18, 4);
        builder.Property(x => x.ProgressPercentage).HasPrecision(18, 4);
        builder.Property(x => x.ProducedQty).HasPrecision(18, 4);
        builder.Property(x => x.RejectedQty).HasPrecision(18, 4);
        builder.HasIndex(x => x.BusinessPeriodId);
    }
}

internal sealed class ProductionMonthlySnapshotConfiguration : IEntityTypeConfiguration<ProductionMonthlySnapshot>
{
    public void Configure(EntityTypeBuilder<ProductionMonthlySnapshot> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.ProductionMonthlySnapshotsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductionOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductionOrderNumber).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TotalQty).HasPrecision(18, 4);
        builder.Property(x => x.CompletedQty).HasPrecision(18, 4);
        builder.Property(x => x.WorkInProgressQty).HasPrecision(18, 4);
        builder.Property(x => x.ProgressPercentage).HasPrecision(18, 4);
        builder.Property(x => x.MaterialConsumed).HasPrecision(18, 4);
        builder.Property(x => x.LaborHours).HasPrecision(18, 4);
        builder.Property(x => x.EstimatedCost).HasPrecision(18, 4);
        builder.Property(x => x.ActualCostToDate).HasPrecision(18, 4);
        builder.Property(x => x.WipCost).HasPrecision(18, 4);
        builder.HasIndex(x => x.MonthlyPeriodId);
    }
}

internal sealed class InventoryDailySnapshotConfiguration : IEntityTypeConfiguration<InventoryDailySnapshot>
{
    public void Configure(EntityTypeBuilder<InventoryDailySnapshot> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.InventoryDailySnapshotsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.InventoryItemId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OpeningQty).HasPrecision(18, 4);
        builder.Property(x => x.Receipts).HasPrecision(18, 4);
        builder.Property(x => x.Returns).HasPrecision(18, 4);
        builder.Property(x => x.ProductionOutput).HasPrecision(18, 4);
        builder.Property(x => x.Issues).HasPrecision(18, 4);
        builder.Property(x => x.Consumption).HasPrecision(18, 4);
        builder.Property(x => x.Deliveries).HasPrecision(18, 4);
        builder.Property(x => x.Adjustments).HasPrecision(18, 4);
        builder.Property(x => x.ClosingQty).HasPrecision(18, 4);
        builder.Property(x => x.MovementIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.BusinessPeriodId);
    }
}

internal sealed class InventoryMonthlySnapshotConfiguration : IEntityTypeConfiguration<InventoryMonthlySnapshot>
{
    public void Configure(EntityTypeBuilder<InventoryMonthlySnapshot> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.InventoryMonthlySnapshotsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InventoryItemId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OpeningQty).HasPrecision(18, 4);
        builder.Property(x => x.OpeningValue).HasPrecision(18, 4);
        builder.Property(x => x.ReceivedQty).HasPrecision(18, 4);
        builder.Property(x => x.ReceivedValue).HasPrecision(18, 4);
        builder.Property(x => x.ConsumedQty).HasPrecision(18, 4);
        builder.Property(x => x.ConsumedValue).HasPrecision(18, 4);
        builder.Property(x => x.AdjustmentQty).HasPrecision(18, 4);
        builder.Property(x => x.AdjustmentValue).HasPrecision(18, 4);
        builder.Property(x => x.ClosingQty).HasPrecision(18, 4);
        builder.Property(x => x.ClosingValue).HasPrecision(18, 4);
        builder.HasIndex(x => x.MonthlyPeriodId);
    }
}

internal sealed class WorkerSessionCheckpointConfiguration : IEntityTypeConfiguration<WorkerSessionCheckpoint>
{
    public void Configure(EntityTypeBuilder<WorkerSessionCheckpoint> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.SessionCheckpointsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SessionId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkerId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.WorkerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProductionOrderId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OperationName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ProgressPercentage).HasPrecision(18, 4);
        builder.Property(x => x.RuleApplied).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.BusinessPeriodId);
    }
}

internal sealed class PeriodAuditLogConfiguration : IEntityTypeConfiguration<PeriodAuditLog>
{
    public void Configure(EntityTypeBuilder<PeriodAuditLog> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.PeriodAuditLogsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PeriodType).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Action).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.UserName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000);
        builder.Property(x => x.DetailsJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => new { x.PeriodType, x.PeriodId });
    }
}

internal sealed class PeriodAdjustmentConfiguration : IEntityTypeConfiguration<PeriodAdjustment>
{
    public void Configure(EntityTypeBuilder<PeriodAdjustment> builder)
    {
        builder.ToTable(PeriodClosePersistenceConstants.PeriodAdjustmentsTableName, PeriodClosePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BranchId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PostingBusinessDate).HasMaxLength(10).IsRequired();
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.EntityId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.OriginalBusinessDate).HasMaxLength(10);
        builder.Property(x => x.OriginalTransactionId).HasMaxLength(100);
        builder.Property(x => x.AdjustmentType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.QuantityDelta).HasPrecision(18, 4);
        builder.Property(x => x.AmountDelta).HasPrecision(18, 4);
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.BranchId);
        builder.HasIndex(x => x.PostingBusinessDate);
    }
}
