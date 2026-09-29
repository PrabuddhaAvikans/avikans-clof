using ATSolution.Domain.Entities.Common;
using Sales.Domain.Common;

namespace Sales.Domain.Quotations;

public class Quotation : Entity<Guid>, IAuditableEntity
{
    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public string Status { get; private set; } = QuotationStatuses.Draft;
    public string Priority { get; private set; } = PriorityValues.Medium;
    public DateTimeOffset ValidUntil { get; private set; }
    public string? Notes { get; private set; }
    public string? Terms { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = SalesDefaults.Currency;
    public string PaymentStatus { get; private set; } = PaymentStatuses.Unpaid;
    public string BillingAddressJson { get; private set; } = "{}";
    public string? ShippingAddressJson { get; private set; }
    public string AttachmentsJson { get; private set; } = "[]";
    public string RevisionsJson { get; private set; } = "[]";
    public string? WorkflowSnapshotJson { get; private set; }
    public Guid? SalesOrderId { get; private set; }
    public string CreatedBy { get; private set; } = SalesDefaults.SystemActor;
    public string CreatedByName { get; private set; } = SalesDefaults.SystemActorName;
    public DateTimeOffset? SentAtUtc { get; private set; }
    public DateTimeOffset? ViewedAtUtc { get; private set; }
    public DateTimeOffset? AcceptedAtUtc { get; private set; }
    public ICollection<QuotationLine> Lines { get; private set; } = new List<QuotationLine>();
    public ICollection<QuotationContact> Contacts { get; private set; } = new List<QuotationContact>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Quotation Create(
        string number,
        Guid customerId,
        string customerName,
        string customerEmail,
        string status,
        string priority,
        DateTimeOffset validUntil,
        string? notes,
        string? terms,
        decimal discountAmount,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string currency,
        string paymentStatus,
        string billingAddressJson,
        string? shippingAddressJson,
        string attachmentsJson,
        string revisionsJson,
        string? workflowSnapshotJson,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new Quotation
        {
            Id = Guid.NewGuid(),
            Number = number,
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            Status = status,
            Priority = priority,
            ValidUntil = validUntil,
            Notes = notes,
            Terms = terms,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = currency,
            PaymentStatus = paymentStatus,
            BillingAddressJson = billingAddressJson,
            ShippingAddressJson = shippingAddressJson,
            AttachmentsJson = attachmentsJson,
            RevisionsJson = revisionsJson,
            WorkflowSnapshotJson = workflowSnapshotJson,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void UpdateHeader(
        string? priority,
        DateTimeOffset? validUntil,
        string? notes,
        string? terms,
        decimal? discountAmount,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string? status,
        string? attachmentsJson,
        string? revisionsJson)
    {
        if (priority is not null) Priority = priority;
        if (validUntil.HasValue) ValidUntil = validUntil.Value;
        if (notes is not null) Notes = notes;
        if (terms is not null) Terms = terms;
        if (discountAmount.HasValue) DiscountAmount = discountAmount.Value;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        if (status is not null) Status = status;
        if (attachmentsJson is not null) AttachmentsJson = attachmentsJson;
        if (revisionsJson is not null) RevisionsJson = revisionsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkSent()
    {
        Status = QuotationStatuses.Sent;
        SentAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void MarkConverted(Guid salesOrderId)
    {
        Status = QuotationStatuses.Converted;
        SalesOrderId = salesOrderId;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;
}
