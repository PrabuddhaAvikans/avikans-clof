using ATSolution.SharedKernel.Models;

namespace Finance.Application.Invoices;

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
