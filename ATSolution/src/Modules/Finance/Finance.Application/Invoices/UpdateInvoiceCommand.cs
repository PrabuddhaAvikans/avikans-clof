using ATSolution.SharedKernel.Models;

namespace Finance.Application.Invoices;

public sealed record UpdateInvoiceCommand(
    Guid Id,
    DateTimeOffset? IssueDate = null,
    DateTimeOffset? DueDate = null,
    IReadOnlyList<InvoiceLineItemInputDto>? LineItems = null,
    string? Currency = null,
    string? Notes = null);
