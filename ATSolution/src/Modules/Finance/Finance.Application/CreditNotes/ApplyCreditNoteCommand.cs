using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

public sealed record ApplyCreditNoteCommand(
    Guid Id,
    Guid InvoiceId,
    decimal Amount,
    string? Note = null,
    string? AppliedBy = null,
    string? AppliedByName = null);
