using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country);
