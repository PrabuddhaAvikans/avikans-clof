using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

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
