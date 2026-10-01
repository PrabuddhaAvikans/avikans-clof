using ATSolution.SharedKernel.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using Sales.Domain.Sequences;

namespace Sales.Infrastructure.Persistence.Configurations;

internal sealed class QuotationConfiguration : IEntityTypeConfiguration<Quotation>
{
    public void Configure(EntityTypeBuilder<Quotation> builder)
    {
        builder.ToTable(SalesPersistenceConstants.QuotationsTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Terms).HasMaxLength(4000);
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 4);
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.PaymentStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillingAddressJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ShippingAddressJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.AttachmentsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.RevisionsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.WorkflowSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Lines).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Contacts).WithOne(x => x.Quotation).HasForeignKey(x => x.QuotationId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class QuotationLineConfiguration : IEntityTypeConfiguration<QuotationLine>
{
    public void Configure(EntityTypeBuilder<QuotationLine> builder)
    {
        builder.ToTable(SalesPersistenceConstants.QuotationLinesTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        // Client assigns Guid in QuotationLine.Create — never treat as store-generated.
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductVersionLabel).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.DiscountPercent).HasPrecision(18, 4);
        builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 4);
        builder.Property(x => x.CustomizationJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => x.QuotationId);
    }
}

internal sealed class QuotationContactConfiguration : IEntityTypeConfiguration<QuotationContact>
{
    public void Configure(EntityTypeBuilder<QuotationContact> builder)
    {
        builder.ToTable(SalesPersistenceConstants.QuotationContactsTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        // Client assigns Guid in QuotationContact.Create — never treat as store-generated.
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Summary).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Detail).HasMaxLength(4000);
        builder.Property(x => x.Outcome).HasMaxLength(500);
        builder.Property(x => x.ContactedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ContactedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.QuotationId);
    }
}

internal sealed class SalesOrderConfiguration : IEntityTypeConfiguration<SalesOrder>
{
    public void Configure(EntityTypeBuilder<SalesOrder> builder)
    {
        builder.ToTable(SalesPersistenceConstants.SalesOrdersTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.QuotationNumber).HasMaxLength(50);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.AssignedToName).HasMaxLength(300);
        builder.Property(x => x.PaymentStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DiscountAmount).HasPrecision(18, 4);
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.BillingAddressJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ShippingAddressJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CancelReason).HasMaxLength(2000);
        builder.Property(x => x.ManufacturingJobIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.DeliveryIdsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.WorkflowSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.LineSnapshotsJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.QuotationId);
        builder.HasMany(x => x.Lines).WithOne(x => x.SalesOrder).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SalesOrderLineConfiguration : IEntityTypeConfiguration<SalesOrderLine>
{
    public void Configure(EntityTypeBuilder<SalesOrderLine> builder)
    {
        builder.ToTable(SalesPersistenceConstants.SalesOrderLinesTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProductSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProductVersionLabel).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.DiscountPercent).HasPrecision(18, 4);
        builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 4);
        builder.Property(x => x.QuantityDelivered).HasPrecision(18, 4);
        builder.Property(x => x.QuantityInManufacturing).HasPrecision(18, 4);
        builder.Property(x => x.CustomizationJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.BomSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CostSnapshotJson).HasColumnType("nvarchar(max)");
        builder.HasIndex(x => x.SalesOrderId);
    }
}

internal sealed class CostingRequestConfiguration : IEntityTypeConfiguration<CostingRequest>
{
    public void Configure(EntityTypeBuilder<CostingRequest> builder)
    {
        builder.ToTable(SalesPersistenceConstants.CostingRequestsTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SalesOrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.QuotationNumber).HasMaxLength(50);
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ProjectName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.RequestType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.TotalEstimate).HasPrecision(18, 4);
        builder.Property(x => x.ProposedPrice).HasPrecision(18, 4);
        builder.Property(x => x.MarginPercent).HasPrecision(18, 4);
        builder.Property(x => x.TargetMargin).HasPrecision(18, 4);
        builder.Property(x => x.RiskFlag).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SlaRemaining).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CoatingStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.PaymentTerms).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LineItemsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CoatingItemsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.EstimationMaterialsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.EstimationProductLinesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AttachmentsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.Notes).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.RequesterJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ApprovalLevelsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.HistoryJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CommentsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ConfigSnapshotJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.WorkflowDefinitionId).HasMaxLength(100);
        builder.Property(x => x.WorkflowVersionId).HasMaxLength(100);
        builder.Property(x => x.WorkflowInstanceId).HasMaxLength(100);
        builder.Property(x => x.WorkflowName).HasMaxLength(200);
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.SalesOrderId).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}

internal sealed class DocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable(SalesPersistenceConstants.DocumentSequencesTableName, SalesPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.DocumentType, x.Year }).IsUnique();
    }
}
