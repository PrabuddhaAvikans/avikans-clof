using ATSolution.Domain.Entities.Common;
using Finance.Domain.Common;

namespace Finance.Domain.Invoices;

public class Invoice : Entity<Guid>, IAuditableEntity
{
    public string InvoiceNumber { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public Guid? SalesOrderId { get; private set; }
    public string? SalesOrderNumber { get; private set; }
    public string Status { get; private set; } = InvoiceStatuses.Draft;
    public DateTimeOffset IssueDate { get; private set; }
    public DateTimeOffset DueDate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public decimal AmountPaid { get; private set; }
    public decimal AmountCredited { get; private set; }
    public decimal OutstandingAmount { get; private set; }
    public string Currency { get; private set; } = FinanceDefaults.Currency;
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = FinanceDefaults.SystemActor;
    public string CreatedByName { get; private set; } = FinanceDefaults.SystemActorName;
    public ICollection<InvoiceLine> LineItems { get; private set; } = new List<InvoiceLine>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Invoice Create(
        string invoiceNumber,
        Guid customerId,
        string customerName,
        string customerEmail,
        Guid? salesOrderId,
        string? salesOrderNumber,
        DateTimeOffset issueDate,
        DateTimeOffset dueDate,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string currency,
        string? notes,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new Invoice
        {
            Id = Guid.NewGuid(),
            InvoiceNumber = invoiceNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            SalesOrderId = salesOrderId,
            SalesOrderNumber = salesOrderNumber,
            Status = InvoiceStatuses.Draft,
            IssueDate = issueDate,
            DueDate = dueDate,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            AmountPaid = 0,
            AmountCredited = 0,
            OutstandingAmount = totalAmount,
            Currency = currency,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateDraft(
        DateTimeOffset? issueDate,
        DateTimeOffset? dueDate,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string? currency,
        string? notes)
    {
        EnsureEditable();
        if (issueDate.HasValue) IssueDate = issueDate.Value;
        if (dueDate.HasValue) DueDate = dueDate.Value;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        if (currency is not null) Currency = currency;
        if (notes is not null) Notes = notes;
        RecalculateOutstanding();
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Issue(DateTimeOffset? issueDate = null)
    {
        if (!InvoiceStatuses.CanIssue(Status))
            throw new InvalidOperationException($"Cannot issue invoice in status '{Status}'.");

        Status = InvoiceStatuses.Issued;
        if (issueDate.HasValue) IssueDate = issueDate.Value;
        RecalculateOutstanding();
        RefreshPaymentStatus();
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Void()
    {
        if (!InvoiceStatuses.CanVoid(Status))
            throw new InvalidOperationException($"Cannot void invoice in status '{Status}'.");

        Status = InvoiceStatuses.Void;
        OutstandingAmount = 0;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RecordPayment(decimal amount)
    {
        if (!InvoiceStatuses.CanRecordPayment(Status))
            throw new InvalidOperationException($"Cannot record payment on invoice in status '{Status}'.");

        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be greater than zero.");

        if (amount > OutstandingAmount)
            throw new InvalidOperationException("Payment amount exceeds outstanding balance.");

        AmountPaid += amount;
        RecalculateOutstanding();
        RefreshPaymentStatus();
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void ApplyCredit(decimal amount)
    {
        if (Status is InvoiceStatuses.Void or InvoiceStatuses.Draft)
            throw new InvalidOperationException($"Cannot apply credit to invoice in status '{Status}'.");

        if (amount <= 0)
            throw new InvalidOperationException("Credit amount must be greater than zero.");

        if (amount > OutstandingAmount)
            throw new InvalidOperationException("Credit amount exceeds outstanding balance.");

        AmountCredited += amount;
        RecalculateOutstanding();
        RefreshPaymentStatus();
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;

    private void EnsureEditable()
    {
        if (!InvoiceStatuses.IsEditable(Status))
            throw new InvalidOperationException("Only draft invoices can be updated.");
    }

    private void RecalculateOutstanding()
    {
        OutstandingAmount = Math.Max(0, TotalAmount - AmountPaid - AmountCredited);
    }

    private void RefreshPaymentStatus()
    {
        if (Status == InvoiceStatuses.Void)
            return;

        if (OutstandingAmount <= 0 && TotalAmount > 0)
        {
            Status = InvoiceStatuses.Paid;
            return;
        }

        if (AmountPaid > 0 || AmountCredited > 0)
        {
            Status = InvoiceStatuses.Partial;
            return;
        }

        if (Status is InvoiceStatuses.Issued or InvoiceStatuses.Overdue or InvoiceStatuses.Partial)
        {
            Status = DueDate < DateTimeOffset.UtcNow
                ? InvoiceStatuses.Overdue
                : InvoiceStatuses.Issued;
        }
    }
}
