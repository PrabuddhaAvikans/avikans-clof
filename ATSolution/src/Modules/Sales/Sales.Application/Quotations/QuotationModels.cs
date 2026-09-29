using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Quotations;

public sealed class QuotationListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public string? Priority { get; set; }
}

public sealed record AddressDto(
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country);

public sealed record AttachmentDto(
    Guid Id,
    string Name,
    long Size,
    string? ContentType,
    string? Url);

public sealed record QuotationContactDto(
    Guid Id,
    string Type,
    string Summary,
    string? Detail,
    string ContactedBy,
    string ContactedByName,
    DateTimeOffset ContactedAt,
    string? Outcome);

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

public sealed record QuotationLineDto(
    Guid Id,
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    Guid? ProductVersionId,
    string? ProductVersionLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    decimal LineTotal,
    bool IsCustomized,
    JsonElement? Customization,
    bool RequiresManufacturing);

public sealed record QuotationDto(
    Guid Id,
    string QuotationNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    string Status,
    string Priority,
    IReadOnlyList<QuotationLineDto> LineItems,
    decimal Subtotal,
    decimal DiscountAmount,
    decimal TaxAmount,
    decimal TotalAmount,
    string Currency,
    DateTimeOffset ValidUntil,
    string PaymentStatus,
    AddressDto BillingAddress,
    AddressDto? ShippingAddress,
    string? Notes,
    string? TermsAndConditions,
    IReadOnlyList<AttachmentDto> Attachments,
    Guid? SalesOrderId,
    IReadOnlyList<QuotationContactDto> ContactHistory,
    IReadOnlyList<QuotationRevisionDto> Revisions,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset? SentAt,
    DateTimeOffset? ViewedAt,
    DateTimeOffset? AcceptedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record QuotationLineInputDto(
    Guid? ProductId,
    string ProductSku,
    string ProductName,
    string? Description,
    Guid? ProductVersionId,
    string? ProductVersionLabel,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountPercent,
    decimal TaxPercent,
    bool? IsCustomized,
    JsonElement? Customization,
    bool? RequiresManufacturing);

public sealed record CreateQuotationCommand(
    Guid CustomerId,
    IReadOnlyList<QuotationLineInputDto> LineItems,
    DateTimeOffset ValidUntil,
    string Priority,
    string? Notes,
    string? TermsAndConditions,
    decimal? DiscountAmount,
    string? Status,
    IReadOnlyList<AttachmentDto>? Attachments,
    string? SaveMode,
    string? CreatedBy,
    string? CreatedByName,
    JsonElement? WorkflowSnapshot);

public sealed record UpdateQuotationCommand(
    Guid Id,
    Guid? CustomerId = null,
    IReadOnlyList<QuotationLineInputDto>? LineItems = null,
    DateTimeOffset? ValidUntil = null,
    string? Priority = null,
    string? Notes = null,
    string? TermsAndConditions = null,
    decimal? DiscountAmount = null,
    string? Status = null,
    IReadOnlyList<AttachmentDto>? Attachments = null,
    string? SaveMode = null);

public sealed record AddQuotationContactCommand(
    Guid QuotationId,
    string Type,
    string Summary,
    string? Detail,
    string? Outcome,
    string? ContactedBy,
    string? ContactedByName);

public sealed record PromoteCustomizationResultDto(QuotationDto Quotation, object Product);
