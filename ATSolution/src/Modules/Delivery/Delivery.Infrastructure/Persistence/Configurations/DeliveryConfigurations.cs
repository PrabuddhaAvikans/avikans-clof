using ATSolution.SharedKernel.Constants;
using Delivery.Domain.Sequences;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Delivery.Infrastructure.Persistence.Configurations;

internal sealed class DeliveryConfiguration : IEntityTypeConfiguration<DeliveryEntity>
{
    public void Configure(EntityTypeBuilder<DeliveryEntity> builder)
    {
        builder.ToTable(DeliveryPersistenceConstants.DeliveriesTableName, DeliveryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Number).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SalesOrderNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Priority).HasMaxLength(50).IsRequired();
        builder.Property(x => x.DriverName).HasMaxLength(300);
        builder.Property(x => x.Vehicle).HasMaxLength(100);
        builder.Property(x => x.Carrier).HasMaxLength(200);
        builder.Property(x => x.TrackingNumber).HasMaxLength(100);
        builder.Property(x => x.LineItemsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ShippingAddressJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ProofOfDeliveryJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.Number).IsUnique();
        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.DriverUserId);
    }
}

internal sealed class DeliveryDocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable(
            DeliveryPersistenceConstants.DocumentSequencesTableName,
            DeliveryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.DocumentType, x.Year }).IsUnique();
    }
}
