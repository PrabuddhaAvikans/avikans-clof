using ATSolution.SharedKernel.Constants;
using Catalog.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Infrastructure.Persistence.Configurations;

internal sealed class ProductVersionConfiguration : IEntityTypeConfiguration<ProductVersion>
{
    public void Configure(EntityTypeBuilder<ProductVersion> builder)
    {
        builder.ToTable(CatalogPersistenceConstants.ProductVersionsTableName, CatalogPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Label).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SpecificationsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.BomJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.OperationsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.AttributesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ImagesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.CostBreakdownJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TagsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.SellingPrice).HasPrecision(18, 4);
        builder.Property(x => x.CostPrice).HasPrecision(18, 4);
        builder.Property(x => x.MarginPercent).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(2000);
        builder.Property(x => x.RevisionNotes).HasMaxLength(2000);
        builder.Property(x => x.ReleasedBy).HasMaxLength(200);

        builder.HasIndex(x => new { x.ProductId, x.VersionNumber }).IsUnique();
    }
}
