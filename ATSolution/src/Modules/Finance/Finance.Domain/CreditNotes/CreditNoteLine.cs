using ATSolution.Domain.Entities.Common;

namespace Finance.Domain.CreditNotes;

public class CreditNoteLine : Entity<Guid>
{
    public Guid CreditNoteId { get; private set; }
    public CreditNote CreditNote { get; private set; } = null!;
    public Guid? ProductId { get; private set; }
    public string? ProductSku { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal TaxPercent { get; private set; }
    public decimal LineTotal { get; private set; }
    public int SortOrder { get; private set; }

    public static CreditNoteLine Create(
        Guid creditNoteId,
        Guid? productId,
        string? productSku,
        string productName,
        string? description,
        decimal quantity,
        decimal unitPrice,
        decimal taxPercent,
        decimal lineTotal,
        int sortOrder)
    {
        return new CreditNoteLine
        {
            Id = Guid.NewGuid(),
            CreditNoteId = creditNoteId,
            ProductId = productId,
            ProductSku = productSku,
            ProductName = productName,
            Description = description,
            Quantity = quantity,
            UnitPrice = unitPrice,
            TaxPercent = taxPercent,
            LineTotal = lineTotal,
            SortOrder = sortOrder,
        };
    }
}
