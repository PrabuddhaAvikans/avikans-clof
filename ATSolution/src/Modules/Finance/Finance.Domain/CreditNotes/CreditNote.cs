using ATSolution.Domain.Entities.Common;
using Finance.Domain.Common;

namespace Finance.Domain.CreditNotes;

public class CreditNote : Entity<Guid>, IAuditableEntity
{
    public string CreditNoteNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public Guid? InvoiceId { get; private set; }
    public string? InvoiceNumber { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public string? SalesOrderNumber { get; private set; }
    public string Status { get; private set; } = CreditNoteStatuses.Draft;
    public string Reason { get; private set; } = CreditNoteReasons.Other;
    public DateTimeOffset? IssueDate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AppliedAmount { get; private set; }
    public decimal RemainingAmount { get; private set; }
    public string Currency { get; private set; } = FinanceDefaults.Currency;
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = FinanceDefaults.SystemActor;
    public string CreatedByName { get; private set; } = FinanceDefaults.SystemActorName;
    public string? IssuedBy { get; private set; }
    public string? IssuedByName { get; private set; }
    public ICollection<CreditNoteLine> LineItems { get; private set; } = new List<CreditNoteLine>();
    public ICollection<CreditNoteApplication> Applications { get; private set; } = new List<CreditNoteApplication>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static CreditNote Create(
        string creditNoteNumber,
        Guid customerId,
        string customerName,
        string customerEmail,
        Guid? invoiceId,
        string? invoiceNumber,
        Guid? salesOrderId,
        string? salesOrderNumber,
        string reason,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string currency,
        string? notes,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new CreditNote
        {
            Id = Guid.NewGuid(),
            CreditNoteNumber = creditNoteNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            SalesOrderId = salesOrderId,
            SalesOrderNumber = salesOrderNumber,
            Status = CreditNoteStatuses.Draft,
            Reason = reason,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            AppliedAmount = 0,
            RemainingAmount = totalAmount,
            Currency = currency,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateDraft(
        string? reason,
        Guid? invoiceId,
        string? invoiceNumber,
        Guid? salesOrderId,
        string? salesOrderNumber,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string? currency,
        string? notes)
    {
        EnsureEditable();
        if (reason is not null) Reason = reason;
        if (invoiceId.HasValue) InvoiceId = invoiceId;
        if (invoiceNumber is not null) InvoiceNumber = invoiceNumber;
        if (salesOrderId.HasValue) SalesOrderId = salesOrderId;
        if (salesOrderNumber is not null) SalesOrderNumber = salesOrderNumber;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        RemainingAmount = Math.Max(0, totalAmount - AppliedAmount);
        if (currency is not null) Currency = currency;
        if (notes is not null) Notes = notes;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Issue(string issuedBy, string issuedByName, DateTimeOffset? issueDate = null)
    {
        if (!CreditNoteStatuses.CanIssue(Status))
            throw new InvalidOperationException($"Cannot issue credit note in status '{Status}'.");

        Status = CreditNoteStatuses.Issued;
        IssueDate = issueDate ?? DateTimeOffset.UtcNow;
        IssuedBy = issuedBy;
        IssuedByName = issuedByName;
        RemainingAmount = Math.Max(0, TotalAmount - AppliedAmount);
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        if (!CreditNoteStatuses.CanVoid(Status))
            throw new InvalidOperationException($"Cannot void credit note in status '{Status}'.");

        if (AppliedAmount > 0)
            throw new InvalidOperationException("Cannot void a credit note that has applications.");

        Status = CreditNoteStatuses.Void;
        RemainingAmount = 0;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public CreditNoteApplication ApplyToInvoice(
        Guid invoiceId,
        string invoiceNumber,
        decimal amount,
        string note,
        string appliedBy,
        string appliedByName)
    {
        if (!CreditNoteStatuses.CanApply(Status))
            throw new InvalidOperationException($"Cannot apply credit note in status '{Status}'.");

        if (amount <= 0)
            throw new InvalidOperationException("Application amount must be greater than zero.");

        if (amount > RemainingAmount)
            throw new InvalidOperationException("Application amount exceeds remaining credit.");

        var application = CreditNoteApplication.Create(
            Id,
            invoiceId,
            invoiceNumber,
            amount,
            note,
            appliedBy,
            appliedByName);

        Applications.Add(application);
        AppliedAmount += amount;
        RemainingAmount = Math.Max(0, TotalAmount - AppliedAmount);
        RefreshApplicationStatus();
        ModifiedOnUtc = DateTimeOffset.UtcNow;
        return application;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;

    private void EnsureEditable()
    {
        if (!CreditNoteStatuses.IsEditable(Status))
            throw new InvalidOperationException("Only draft credit notes can be updated.");
    }

    private void RefreshApplicationStatus()
    {
        if (Status == CreditNoteStatuses.Void)
            return;

        if (RemainingAmount <= 0)
            Status = CreditNoteStatuses.Applied;
        else if (AppliedAmount > 0)
            Status = CreditNoteStatuses.PartiallyApplied;
        else
            Status = CreditNoteStatuses.Issued;
    }
}
