using ATSolution.SharedKernel.Models;

namespace Customers.Application.Customers;

public sealed record UpdateCustomerCommand(
    Guid Id,
    string? Code = null,
    string? Name = null,
    string? Type = null,
    string? Email = null,
    string? Phone = null,
    IReadOnlyList<AddressDto>? BillingAddresses = null,
    int? ActiveBillingAddressIndex = null,
    bool? DeliverySameAsBilling = null,
    IReadOnlyList<AddressDto>? ShippingAddresses = null,
    int? ActiveShippingAddressIndex = null,
    IReadOnlyList<ContactPersonInputDto>? ContactPersons = null,
    string? TaxId = null,
    decimal? CreditLimit = null,
    int? PaymentTermsDays = null,
    string? Notes = null,
    string? Status = null);
