using ATSolution.SharedKernel.Models;

namespace Finance.Application.Invoices;

public sealed class InvoiceListQuery : PaginatedRequest
{
    public string? Status { get; set; }
    public Guid? CustomerId { get; set; }
}

public sealed record InvoiceLineItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal LineTotal);

public sealed record InvoiceDto(
    Guid Id,
    string InvoiceNumber,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    string Status,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    IReadOnlyList<InvoiceLineItemDto> LineItems,
    decimal Subtotal,
    decimal TaxAmount,
    decimal TotalAmount,
    decimal AmountPaid,
    decimal AmountCredited,
    decimal OutstandingAmount,
    string Currency,
    string? Notes,
    string CreatedBy,
    string CreatedByName,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record InvoiceLineItemInputDto(
    Guid? Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal = null);

public sealed record CreateInvoiceCommand(
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? SalesOrderId,
    string? SalesOrderNumber,
    DateTimeOffset IssueDate,
    DateTimeOffset DueDate,
    IReadOnlyList<InvoiceLineItemInputDto> LineItems,
    string? Currency,
    string? Notes,
    string? CreatedBy,
    string? CreatedByName);

public sealed record UpdateInvoiceCommand(
    Guid Id,
    DateTimeOffset? IssueDate = null,
    DateTimeOffset? DueDate = null,
    IReadOnlyList<InvoiceLineItemInputDto>? LineItems = null,
    string? Currency = null,
    string? Notes = null);

public sealed record RecordInvoicePaymentCommand(
    Guid Id,
    decimal Amount);

public sealed record IssueInvoiceCommand(Guid Id);

public sealed record VoidInvoiceCommand(Guid Id);
