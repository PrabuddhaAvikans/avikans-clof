namespace Sales.Api.DTOs.Requests;

public sealed record AddQuotationContactRequestDto(
    string Type,
    string Summary,
    string? Detail,
    string? Outcome,
    string? ContactedBy,
    string? ContactedByName);
