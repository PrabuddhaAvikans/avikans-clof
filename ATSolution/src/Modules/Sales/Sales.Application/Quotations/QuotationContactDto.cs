using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record QuotationContactDto(
    Guid Id,
    string Type,
    string Summary,
    string? Detail,
    string ContactedBy,
    string ContactedByName,
    DateTimeOffset ContactedAt,
    string? Outcome);
