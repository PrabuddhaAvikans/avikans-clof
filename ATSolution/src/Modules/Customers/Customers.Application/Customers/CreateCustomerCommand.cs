using ATSolution.SharedKernel.Models;

namespace Customers.Application.Customers;

public sealed record CreateCustomerCommand(
    string? Code,
    string Name,
    string Type,
    string Email,
    string Phone,
    IReadOnlyList<AddressDto> BillingAddresses,
    int ActiveBillingAddressIndex,
    bool DeliverySameAsBilling,
    IReadOnlyList<AddressDto>? ShippingAddresses,
    int? ActiveShippingAddressIndex,
    IReadOnlyList<ContactPersonInputDto> ContactPersons,
    string? TaxId,
    decimal? CreditLimit,
    int PaymentTermsDays,
    string? Notes,
    string Status);
