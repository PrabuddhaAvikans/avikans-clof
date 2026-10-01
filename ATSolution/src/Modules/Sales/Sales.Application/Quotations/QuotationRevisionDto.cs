using System.Text.Json;
namespace Sales.Application.Quotations;

public sealed record QuotationRevisionDto(
    Guid Id,
    int VersionNumber,
    string Label,
    bool IsCurrent,
    bool IsDraft,
    decimal TotalAmount,
    string Currency,
    string? Notes,
    DateTimeOffset CreatedAt,
    string CreatedBy,
    string CreatedByName);
