using ATSolution.SharedKernel.Constants;
using Customers.Domain.Customers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Customers.Infrastructure.Persistence.Configurations;

internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable(CustomersPersistenceConstants.CustomersTableName, CustomersPersistenceConstants.SchemaName);
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(50).IsRequired();
        builder.Property(x => x.BillingAddressesJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ShippingAddressesJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.ContactPersonsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.TaxId).HasMaxLength(100);
        builder.Property(x => x.CreditLimit).HasPrecision(18, 4);
        builder.Property(x => x.TotalRevenue).HasPrecision(18, 4);
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.Email);
    }
}
