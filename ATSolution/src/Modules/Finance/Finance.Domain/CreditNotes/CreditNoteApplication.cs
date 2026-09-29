using ATSolution.Domain.Entities.Common;

namespace Finance.Domain.CreditNotes;

public class CreditNoteApplication : Entity<Guid>
{
    public Guid CreditNoteId { get; private set; }
    public CreditNote CreditNote { get; private set; } = null!;
    public Guid InvoiceId { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public string Note { get; private set; } = string.Empty;
    public DateTimeOffset AppliedAt { get; private set; }
    public string AppliedBy { get; private set; } = string.Empty;
    public string AppliedByName { get; private set; } = string.Empty;

    public static CreditNoteApplication Create(
        Guid creditNoteId,
        Guid invoiceId,
        string invoiceNumber,
        decimal amount,
        string note,
        string appliedBy,
        string appliedByName)
    {
        return new CreditNoteApplication
        {
            Id = Guid.NewGuid(),
            CreditNoteId = creditNoteId,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            Amount = amount,
            Note = note,
            AppliedAt = DateTimeOffset.UtcNow,
            AppliedBy = appliedBy,
            AppliedByName = appliedByName,
        };
    }
}
