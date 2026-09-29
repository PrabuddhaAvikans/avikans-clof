using ATSolution.Domain.Entities.Common;

namespace Finance.Domain.Invoices;

public class InvoiceLine : Entity<Guid>
{
    public Guid InvoiceId { get; private set; }
    public Invoice Invoice { get; private set; } = null!;
    public Guid ProductId { get; private set; }
    public string ProductSku { get; private set; } = string.Empty;
    public string ProductName { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public int SortOrder { get; private set; }

    public static InvoiceLine Create(
        Guid invoiceId,
        Guid productId,
        string productSku,
        string productName,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        decimal lineTotal,
        int sortOrder)
    {
        return new InvoiceLine
        {
            Id = Guid.NewGuid(),
            InvoiceId = invoiceId,
            ProductId = productId,
            ProductSku = productSku,
            ProductName = productName,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxPercent = taxPercent,
            LineTotal = lineTotal,
            SortOrder = sortOrder,
        };
    }
}
