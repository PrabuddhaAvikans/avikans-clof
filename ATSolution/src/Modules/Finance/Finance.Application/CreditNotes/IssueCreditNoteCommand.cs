using ATSolution.SharedKernel.Models;

namespace Finance.Application.CreditNotes;

public sealed record IssueCreditNoteCommand(
    Guid Id,
    string? IssuedBy = null,
    string? IssuedByName = null);
