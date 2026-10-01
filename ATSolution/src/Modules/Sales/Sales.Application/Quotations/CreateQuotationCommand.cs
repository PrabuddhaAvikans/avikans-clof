using System.Text.Json.Nodes;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Quotations;

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
    JsonNode? WorkflowSnapshot = null);
