using ATSolution.SharedKernel.Constants;
using Finance.Domain.CreditNotes;
using Finance.Domain.Invoices;
using Finance.Domain.Sequences;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Finance.Infrastructure.Persistence.Configurations;

internal sealed class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable(FinancePersistenceConstants.InvoicesTableName, FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.SalesOrderNumber).HasMaxLength(50);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.AmountPaid).HasPrecision(18, 4);
        builder.Property(x => x.AmountCredited).HasPrecision(18, 4);
        builder.Property(x => x.OutstandingAmount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.SalesOrderId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.LineItems)
            .WithOne(x => x.Invoice)
            .HasForeignKey(x => x.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InvoiceLineConfiguration : IEntityTypeConfiguration<InvoiceLine>
{
    public void Configure(EntityTypeBuilder<InvoiceLine> builder)
    {
        builder.ToTable(FinancePersistenceConstants.InvoiceLinesTableName, FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductSku).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 4);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.ProductId);
    }
}

internal sealed class CreditNoteConfiguration : IEntityTypeConfiguration<CreditNote>
{
    public void Configure(EntityTypeBuilder<CreditNote> builder)
    {
        builder.ToTable(FinancePersistenceConstants.CreditNotesTableName, FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CreditNoteNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.CustomerName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.CustomerEmail).HasMaxLength(256).IsRequired();
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50);
        builder.Property(x => x.SalesOrderNumber).HasMaxLength(50);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Subtotal).HasPrecision(18, 4);
        builder.Property(x => x.TaxAmount).HasPrecision(18, 4);
        builder.Property(x => x.TotalAmount).HasPrecision(18, 4);
        builder.Property(x => x.AppliedAmount).HasPrecision(18, 4);
        builder.Property(x => x.RemainingAmount).HasPrecision(18, 4);
        builder.Property(x => x.Currency).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.CreatedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CreatedByName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.IssuedBy).HasMaxLength(200);
        builder.Property(x => x.IssuedByName).HasMaxLength(300);
        builder.HasIndex(x => x.CreditNoteNumber).IsUnique();
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.InvoiceId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.LineItems)
            .WithOne(x => x.CreditNote)
            .HasForeignKey(x => x.CreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Applications)
            .WithOne(x => x.CreditNote)
            .HasForeignKey(x => x.CreditNoteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class CreditNoteLineConfiguration : IEntityTypeConfiguration<CreditNoteLine>
{
    public void Configure(EntityTypeBuilder<CreditNoteLine> builder)
    {
        builder.ToTable(FinancePersistenceConstants.CreditNoteLinesTableName, FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProductSku).HasMaxLength(100);
        builder.Property(x => x.ProductName).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).HasPrecision(18, 4);
        builder.Property(x => x.UnitPrice).HasPrecision(18, 4);
        builder.Property(x => x.TaxPercent).HasPrecision(18, 4);
        builder.Property(x => x.LineTotal).HasPrecision(18, 4);
        builder.HasIndex(x => x.CreditNoteId);
        builder.HasIndex(x => x.ProductId);
    }
}

internal sealed class CreditNoteApplicationConfiguration : IEntityTypeConfiguration<CreditNoteApplication>
{
    public void Configure(EntityTypeBuilder<CreditNoteApplication> builder)
    {
        builder.ToTable(FinancePersistenceConstants.CreditNoteApplicationsTableName, FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.InvoiceNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Amount).HasPrecision(18, 4);
        builder.Property(x => x.Note).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AppliedBy).HasMaxLength(200).IsRequired();
        builder.Property(x => x.AppliedByName).HasMaxLength(300).IsRequired();
        builder.HasIndex(x => x.CreditNoteId);
        builder.HasIndex(x => x.InvoiceId);
    }
}

internal sealed class FinanceDocumentSequenceConfiguration : IEntityTypeConfiguration<DocumentSequence>
{
    public void Configure(EntityTypeBuilder<DocumentSequence> builder)
    {
        builder.ToTable(
            FinancePersistenceConstants.DocumentSequencesTableName,
            FinancePersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DocumentType).HasMaxLength(20).IsRequired();
        builder.HasIndex(x => new { x.DocumentType, x.Year }).IsUnique();
    }
}
