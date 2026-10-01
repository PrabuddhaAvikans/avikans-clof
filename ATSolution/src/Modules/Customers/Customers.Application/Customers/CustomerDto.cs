namespace Customers.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Code,
    string Name,
    string Type,
    string Email,
    string Phone,
    IReadOnlyList<AddressDto> BillingAddresses,
    int ActiveBillingAddressIndex,
    bool DeliverySameAsBilling,
    IReadOnlyList<AddressDto>? ShippingAddresses,
    int? ActiveShippingAddressIndex,
    IReadOnlyList<ContactPersonDto> ContactPersons,
    string? TaxId,
    decimal? CreditLimit,
    int PaymentTermsDays,
    string? Notes,
    string Status,
    int TotalOrders,
    decimal TotalRevenue,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
