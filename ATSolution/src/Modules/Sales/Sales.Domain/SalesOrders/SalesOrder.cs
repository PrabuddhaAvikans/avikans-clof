using ATSolution.Domain.Entities.Common;
using Sales.Domain.Common;

namespace Sales.Domain.SalesOrders;

public class SalesOrder : Entity<Guid>, IAuditableEntity
{
    public string Number { get; private set; } = null!;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = null!;
    public string CustomerEmail { get; private set; } = null!;
    public Guid? QuotationId { get; private set; }
    public string? QuotationNumber { get; private set; }
    public Guid? CostingRequestId { get; private set; }
    public string Status { get; private set; } = SalesOrderStatuses.Draft;
    public string Priority { get; private set; } = PriorityValues.Medium;
    public Guid? AssignedToUserId { get; private set; }
    public string? AssignedToName { get; private set; }
    public string PaymentStatus { get; private set; } = PaymentStatuses.Unpaid;
    public decimal DiscountAmount { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TotalAmount { get; private set; }
    public string Currency { get; private set; } = SalesDefaults.Currency;
    public string BillingAddressJson { get; private set; } = "{}";
    public string? ShippingAddressJson { get; private set; }
    public DateTimeOffset? RequestedDeliveryDate { get; private set; }
    public string? Notes { get; private set; }
    public string? CancelReason { get; private set; }
    public string ManufacturingJobIdsJson { get; private set; } = "[]";
    public string DeliveryIdsJson { get; private set; } = "[]";
    public string? WorkflowSnapshotJson { get; private set; }
    public string? LineSnapshotsJson { get; private set; }
    public string CreatedBy { get; private set; } = SalesDefaults.SystemActor;
    public string CreatedByName { get; private set; } = SalesDefaults.SystemActorName;
    public DateTimeOffset? ConfirmedAtUtc { get; private set; }
    public ICollection<SalesOrderLine> Lines { get; private set; } = new List<SalesOrderLine>();
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static SalesOrder Create(
        string number,
        Guid customerId,
        string customerName,
        string customerEmail,
        Guid? quotationId,
        string? quotationNumber,
        string priority,
        decimal discountAmount,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount,
        string currency,
        string billingAddressJson,
        string? shippingAddressJson,
        DateTimeOffset? requestedDeliveryDate,
        string? notes,
        string? workflowSnapshotJson,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new SalesOrder
        {
            Id = Guid.NewGuid(),
            Number = number,
            CustomerId = customerId,
            CustomerName = customerName,
            CustomerEmail = customerEmail,
            QuotationId = quotationId,
            QuotationNumber = quotationNumber,
            Status = SalesOrderStatuses.Draft,
            Priority = priority,
            PaymentStatus = PaymentStatuses.Unpaid,
            DiscountAmount = discountAmount,
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            TotalAmount = totalAmount,
            Currency = currency,
            BillingAddressJson = billingAddressJson,
            ShippingAddressJson = shippingAddressJson,
            RequestedDeliveryDate = requestedDeliveryDate,
            Notes = notes,
            WorkflowSnapshotJson = workflowSnapshotJson,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? priority,
        DateTimeOffset? requestedDeliveryDate,
        string? notes,
        decimal? discountAmount,
        decimal subtotal,
        decimal taxAmount,
        decimal totalAmount)
    {
        if (priority is not null) Priority = priority;
        if (requestedDeliveryDate.HasValue) RequestedDeliveryDate = requestedDeliveryDate;
        if (notes is not null) Notes = notes;
        if (discountAmount.HasValue) DiscountAmount = discountAmount.Value;
        Subtotal = subtotal;
        TaxAmount = taxAmount;
        TotalAmount = totalAmount;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetCostingRequest(Guid costingRequestId)
    {
        CostingRequestId = costingRequestId;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Confirm(string? lineSnapshotsJson)
    {
        Status = SalesOrderStatuses.Confirmed;
        ConfirmedAtUtc = DateTimeOffset.UtcNow;
        if (lineSnapshotsJson is not null) LineSnapshotsJson = lineSnapshotsJson;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Cancel(string? reason)
    {
        Status = SalesOrderStatuses.Cancelled;
        CancelReason = reason;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            Notes = string.IsNullOrWhiteSpace(Notes)
                ? $"Cancelled: {reason}"
                : $"{Notes}\nCancelled: {reason}";
        }
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Assign(Guid userId, string userName)
    {
        AssignedToUserId = userId;
        AssignedToName = userName;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;

    public void SetStatus(string status)
    {
        Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void LinkManufacturingJob(Guid jobId)
    {
        ManufacturingJobIdsJson = AppendGuidJson(ManufacturingJobIdsJson, jobId);
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void LinkDelivery(Guid deliveryId)
    {
        DeliveryIdsJson = AppendGuidJson(DeliveryIdsJson, deliveryId);
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void RefreshDeliveryStatus()
    {
        if (Status is SalesOrderStatuses.Cancelled or SalesOrderStatuses.Draft) return;

        var lines = Lines.Where(l => l.Quantity > 0).ToList();
        if (lines.Count == 0) return;

        var allDelivered = lines.All(l => l.QuantityDelivered >= l.Quantity);
        var anyDelivered = lines.Any(l => l.QuantityDelivered > 0);

        if (allDelivered)
            Status = SalesOrderStatuses.Delivered;
        else if (anyDelivered)
            Status = SalesOrderStatuses.PartiallyDelivered;

        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    private static string AppendGuidJson(string json, Guid id)
    {
        var list = new List<string>();
        if (!string.IsNullOrWhiteSpace(json) && json != "[]")
        {
            try
            {
                var parsed = System.Text.Json.JsonSerializer.Deserialize<List<string>>(json);
                if (parsed is not null) list.AddRange(parsed);
            }
            catch
            {
                // keep empty and rewrite
            }
        }

        var idText = id.ToString();
        if (!list.Contains(idText, StringComparer.OrdinalIgnoreCase))
            list.Add(idText);

        return System.Text.Json.JsonSerializer.Serialize(list);
    }
}
