using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

public sealed class CreditNoteListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? InvoiceId { get; set; }
}

public sealed record CreditNoteLineItemDto(
    Guid Id,
    Guid? ProductId,
    string? ProductSku,
    string ProductName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal LineTotal);

public sealed record CreditNoteApplicationDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string Note,
    DateTimeOffset AppliedAt,
    string AppliedBy,
    string AppliedByName);

public sealed record CreditNoteDto(
    Guid Id,
    string CreditNoteNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    string Status,
    string Reason,
    DateTimeOffset? IssueDate,
    IReadOnlyList<CreditNoteLineItemDto> LineItems,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AppliedAmount,
    decimal RemainingAmount,
    string Currency,
    string? Notes,
    IReadOnlyList<CreditNoteApplicationDto> Applications,
    string CreatedBy,
    string CreatedByName,
    string? IssuedBy,
    string? IssuedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record CreditNoteLineItemInputDto(
    Guid? Id,
    Guid? ProductId,
    string? ProductSku,
    string ProductName,
    string? Description,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal = null);

public sealed record CreateCreditNoteCommand(
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? InvoiceId,
    string? InvoiceNumber,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    string Reason,
    IReadOnlyList<CreditNoteLineItemInputDto> LineItems,
    string? Currency,
    string? Notes,
    string? CreatedBy,
    string? CreatedByName);

public sealed record UpdateCreditNoteCommand(
    Guid Id,
    string? Reason = null,
    Guid? InvoiceId = null,
    string? InvoiceNumber = null,
    Guid? SalesOrderId = null,
    string? SalesOrderNumber = null,
    IReadOnlyList<CreditNoteLineItemInputDto>? LineItems = null,
    string? Currency = null,
    string? Notes = null);

public sealed record IssueCreditNoteCommand(
    Guid Id,
    string? IssuedBy = null,
    string? IssuedByName = null);

public sealed record VoidCreditNoteCommand(Guid Id);

public sealed record ApplyCreditNoteCommand(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string? Note = null,
    string? AppliedBy = null,
    string? AppliedByName = null);
