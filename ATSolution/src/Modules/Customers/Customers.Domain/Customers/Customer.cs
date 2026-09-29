using ATSolution.Domain.Entities.Common;
using Customers.Domain.Common;

namespace Customers.Domain.Customers;

public class Customer : Entity<Guid>, IAuditableEntity
{
    public string Code { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string Type { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Phone { get; private set; } = null!;
    public string BillingAddressesJson { get; private set; } = "[]";
    public int ActiveBillingAddressIndex { get; private set; }
    public bool DeliverySameAsBilling { get; private set; } = true;
    public string? ShippingAddressesJson { get; private set; }
    public int? ActiveShippingAddressIndex { get; private set; }
    public string ContactPersonsJson { get; private set; } = "[]";
    public string? TaxId { get; private set; }
    public decimal? CreditLimit { get; private set; }
    public int PaymentTermsDays { get; private set; }
    public string? Notes { get; private set; }
    public string Status { get; private set; } = EntityStatuses.Active;
    public int TotalOrders { get; private set; }
    public decimal TotalRevenue { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; set; }
    public DateTimeOffset ModifiedOnUtc { get; set; }

    public static Customer Create(
        string code,
        string name,
        string type,
        string email,
        string phone,
        string billingAddressesJson,
        int activeBillingAddressIndex,
        bool deliverySameAsBilling,
        string? shippingAddressesJson,
        int? activeShippingAddressIndex,
        string contactPersonsJson,
        string? taxId,
        decimal? creditLimit,
        int paymentTermsDays,
        string? notes,
        string status)
    {
        var now = DateTimeOffset.UtcNow;
        return new Customer
        {
            Id = Guid.NewGuid(),
            Code = code.Trim(),
            Name = name.Trim(),
            Type = type,
            Email = email.Trim(),
            Phone = phone.Trim(),
            BillingAddressesJson = billingAddressesJson,
            ActiveBillingAddressIndex = activeBillingAddressIndex,
            DeliverySameAsBilling = deliverySameAsBilling,
            ShippingAddressesJson = shippingAddressesJson,
            ActiveShippingAddressIndex = activeShippingAddressIndex,
            ContactPersonsJson = contactPersonsJson,
            TaxId = taxId,
            CreditLimit = creditLimit,
            PaymentTermsDays = paymentTermsDays,
            Notes = notes,
            Status = status,
            CreatedOnUtc = now,
            ModifiedOnUtc = now,
        };
    }

    public void Update(
        string? code,
        string? name,
        string? type,
        string? email,
        string? phone,
        string? billingAddressesJson,
        int? activeBillingAddressIndex,
        bool? deliverySameAsBilling,
        string? shippingAddressesJson,
        int? activeShippingAddressIndex,
        string? contactPersonsJson,
        string? taxId,
        decimal? creditLimit,
        int? paymentTermsDays,
        string? notes,
        string? status)
    {
        if (code is not null) Code = code.Trim();
        if (name is not null) Name = name.Trim();
        if (type is not null) Type = type;
        if (email is not null) Email = email.Trim();
        if (phone is not null) Phone = phone.Trim();
        if (billingAddressesJson is not null) BillingAddressesJson = billingAddressesJson;
        if (activeBillingAddressIndex.HasValue) ActiveBillingAddressIndex = activeBillingAddressIndex.Value;
        if (deliverySameAsBilling.HasValue) DeliverySameAsBilling = deliverySameAsBilling.Value;
        if (shippingAddressesJson is not null) ShippingAddressesJson = shippingAddressesJson;
        if (activeShippingAddressIndex.HasValue) ActiveShippingAddressIndex = activeShippingAddressIndex;
        if (contactPersonsJson is not null) ContactPersonsJson = contactPersonsJson;
        if (taxId is not null) TaxId = taxId;
        if (creditLimit.HasValue) CreditLimit = creditLimit;
        if (paymentTermsDays.HasValue) PaymentTermsDays = paymentTermsDays.Value;
        if (notes is not null) Notes = notes;
        if (status is not null) Status = status;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }

    public void Deactivate()
    {
        Status = EntityStatuses.Inactive;
        ModifiedOnUtc = DateTimeOffset.UtcNow;
    }
}
