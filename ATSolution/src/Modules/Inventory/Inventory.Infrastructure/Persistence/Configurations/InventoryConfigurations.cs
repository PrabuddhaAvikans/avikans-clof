using ATSolution.SharedKernel.Constants;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Inventory.Domain.Reprocessing;
using Inventory.Domain.Units;
using Inventory.Domain.Warehouses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.UnitsOfMeasureTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class WarehouseConfiguration : IEntityTypeConfiguration<Warehouse>
{
    public void Configure(EntityTypeBuilder<Warehouse> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.WarehousesTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(500);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.InventoryItemsTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Sku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Category).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ItemType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Brand).HasMaxLength(200);
        builder.Property(x => x.Supplier).HasMaxLength(200);
        builder.Property(x => x.TaxCode).HasMaxLength(50);
        builder.Property(x => x.QuantityOnHand).HasPrecision(18, 4);
        builder.Property(x => x.QuantityReserved).HasPrecision(18, 4);
        builder.Ignore(x => x.QuantityAvailable);
        builder.Property(x => x.Warehouse).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Location).HasMaxLength(200).IsRequired();
        builder.Property(x => x.MinStock).HasPrecision(18, 4);
        builder.Property(x => x.MaxStock).HasPrecision(18, 4);
        builder.Property(x => x.ReorderLevel).HasPrecision(18, 4);
        builder.Property(x => x.ReorderQuantity).HasPrecision(18, 4);
        builder.Property(x => x.BuyingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.PricingMethod).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MarkupPercent).HasPrecision(18, 4);
        builder.Property(x => x.MarkupFixedAmount).HasPrecision(18, 4);
        builder.Property(x => x.SellingPrice).HasPrecision(18, 4);
        builder.Property(x => x.StockStatus).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();
    }
}

internal sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.StockMovementsTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InventoryItemName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.InventoryItemSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.Unit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.ReferenceId).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.PerformedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.PerformedByName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.TraceJson).HasColumnType("nvarchar(max)");
        builder.HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.InventoryItemId);
        builder.HasIndex(x => x.PerformedAtUtc);
    }
}

internal sealed class InventoryPriceHistoryConfiguration : IEntityTypeConfiguration<InventoryPriceHistory>
{
    public void Configure(EntityTypeBuilder<InventoryPriceHistory> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.InventoryPriceHistoryTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BuyingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.SellingPrice).HasPrecision(18, 4);
        builder.Property(x => x.PricingMethod).HasMaxLength(50).IsRequired();
        builder.Property(x => x.MarkupPercent).HasPrecision(18, 4);
        builder.Property(x => x.MarkupFixedAmount).HasPrecision(18, 4);
        builder.Property(x => x.ChangedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ChangedByName).HasMaxLength(200).IsRequired();
        builder.HasOne(x => x.InventoryItem)
            .WithMany()
            .HasForeignKey(x => x.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(x => x.InventoryItemId);
    }
}

internal sealed class ReprocessingBatchConfiguration : IEntityTypeConfiguration<ReprocessingBatch>
{
    public void Configure(EntityTypeBuilder<ReprocessingBatch> builder)
    {
        builder.ToTable(InventoryPersistenceConstants.ReprocessingBatchesTableName, InventoryPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.BatchNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InputScrapSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.InputScrapName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.InputQuantity).HasPrecision(18, 4);
        builder.Property(x => x.InputUnit).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InputUnitCost).HasPrecision(18, 4);
        builder.Property(x => x.CostLabour).HasPrecision(18, 4);
        builder.Property(x => x.CostElectricity).HasPrecision(18, 4);
        builder.Property(x => x.CostMachine).HasPrecision(18, 4);
        builder.Property(x => x.CostGas).HasPrecision(18, 4);
        builder.Property(x => x.CostFurnace).HasPrecision(18, 4);
        builder.Property(x => x.CostSubcontract).HasPrecision(18, 4);
        builder.Property(x => x.CostOther).HasPrecision(18, 4);
        builder.Property(x => x.TotalProcessingCost).HasPrecision(18, 4);
        builder.Property(x => x.RecoveredQuantity).HasPrecision(18, 4);
        builder.Property(x => x.ProcessLossQuantity).HasPrecision(18, 4);
        builder.Property(x => x.RecoveredUnitCost).HasPrecision(18, 4);
        builder.Property(x => x.RecoveredLotSku).HasMaxLength(100);
        builder.Property(x => x.SourceProductionOrderId).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.CreatedBy).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => x.BatchNumber).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.InputScrapLotId);
    }
}
