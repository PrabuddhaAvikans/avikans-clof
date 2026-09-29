using ATSolution.Domain.Entities.Common;
using Delivery.Domain.Common;

namespace Delivery.Domain.Deliveries;

public class Delivery : Entity<Guid>, IAuditableEntity
{
    public string Number { get; private set; } = null!;
    public Guid SalesOrderId { get; private set; }
    public string SalesOrderNumber { get; private set; } = string.Empty;
    public Guid CustomerId { get; private set; }
    public string CustomerName { get; private set; } = string.Empty;
    public string Status { get; private set; } = DeliveryStatuses.Planned;
    public string Priority { get; private set; } = PriorityValues.Medium;
    public DateTimeOffset ScheduledDate { get; private set; }
    public DateTimeOffset? DispatchedAtUtc { get; private set; }
    public DateTimeOffset? DeliveredAtUtc { get; private set; }
    public Guid? DriverUserId { get; private set; }
    public string? DriverName { get; private set; }
    public string? Vehicle { get; private set; }
    public string? Carrier { get; private set; }
    public string? TrackingNumber { get; private set; }
    public string LineItemsJson { get; private set; } = "[]";
    public string? ShippingAddressJson { get; private set; }
    public string? ProofOfDeliveryJson { get; private set; }
    public string? Notes { get; private set; }
    public string CreatedBy { get; private set; } = "system";
    public string CreatedByName { get; private set; } = "System";
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Delivery Create(
        string number,
        Guid salesOrderId,
        string salesOrderNumber,
        Guid customerId,
        string customerName,
        string priority,
        DateTimeOffset scheduledDate,
        string lineItemsJson,
        string? shippingAddressJson,
        Guid? driverUserId,
        string? driverName,
        string? vehicle,
        string? carrier,
        string? notes,
        string createdBy,
        string createdByName)
    {
        var now = DateTimeOffset.UtcNow;
        return new Delivery
        {
            Id = Guid.NewGuid(),
            Number = number,
            SalesOrderId = salesOrderId,
            SalesOrderNumber = salesOrderNumber,
            CustomerId = customerId,
            CustomerName = customerName,
            Status = DeliveryStatuses.Planned,
            Priority = priority,
            ScheduledDate = scheduledDate,
            LineItemsJson = lineItemsJson,
            ShippingAddressJson = shippingAddressJson,
            DriverUserId = driverUserId,
            DriverName = driverName,
            Vehicle = vehicle,
            Carrier = carrier,
            Notes = notes,
            CreatedBy = createdBy,
            CreatedByName = createdByName,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? priority,
        DateTimeOffset? scheduledDate,
        string? lineItemsJson,
        Guid? driverUserId,
        string? driverName,
        string? vehicle,
        string? carrier,
        string? notes)
    {
        if (priority is not null) Priority = priority;
        if (scheduledDate.HasValue) ScheduledDate = scheduledDate.Value;
        if (lineItemsJson is not null) LineItemsJson = lineItemsJson;
        if (driverUserId.HasValue) DriverUserId = driverUserId;
        if (driverName is not null) DriverName = driverName;
        if (vehicle is not null) Vehicle = vehicle;
        if (carrier is not null) Carrier = carrier;
        if (notes is not null) Notes = notes;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void SetStatus(string status)
    {
        if (!DeliveryStatuses.CanTransition(Status, status) && Status != status)
            throw new InvalidOperationException($"Cannot move delivery from {Status} to {status}.");

        Status = status;
        var now = DateTimeOffset.UtcNow;
        if (status is DeliveryStatuses.Dispatched or DeliveryStatuses.InTransit)
            DispatchedAtUtc ??= now;
        if (status == DeliveryStatuses.Delivered)
            DeliveredAtUtc ??= now;
        ModifiedOnUtc = now;
    }

    public void Dispatch()
    {
        if (Status is not (DeliveryStatuses.Planned or DeliveryStatuses.ReadyForDispatch))
            throw new InvalidOperationException("Only planned or ready deliveries can be dispatched.");

        Status = DeliveryStatuses.Dispatched;
        DispatchedAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = DispatchedAtUtc.Value;
    }

    public void RecordProofOfDelivery(string proofJson)
    {
        ProofOfDeliveryJson = proofJson;
        Status = DeliveryStatuses.Delivered;
        DeliveredAtUtc = DateTimeOffset.UtcNow;
        ModifiedOnUtc = DeliveredAtUtc.Value;
    }

    public void Touch() => ModifiedOnUtc = DateTimeOffset.UtcNow;
}
