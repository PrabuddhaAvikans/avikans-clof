using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

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
