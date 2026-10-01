using System.Text.Json;
using ATSolution.SharedKernel.Models;

namespace Sales.Application.Quotations;

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
    string? RejectionReason = null,
    IReadOnlyList<AttachmentDto>? Attachments = null,
    string? SaveMode = null);
