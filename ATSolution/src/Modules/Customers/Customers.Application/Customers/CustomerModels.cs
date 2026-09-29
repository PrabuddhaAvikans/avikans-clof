using ATSolution.SharedKernel.Models;

namespace Customers.Application.Customers;

public sealed class CustomerListQuery : PaginatedRequest
{
    public string? Type { get; set; }
    public string? Status { get; set; }
}

public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record ContactPersonDto(
    Guid Id,
    string Name,
    string? Title,
    string Email,
    string Phone,
    bool IsPrimary);

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

public sealed record ContactPersonInputDto(
    Guid? Id,
    string Name,
    string? Title,
    string Email,
    string Phone,
    bool IsPrimary);

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
