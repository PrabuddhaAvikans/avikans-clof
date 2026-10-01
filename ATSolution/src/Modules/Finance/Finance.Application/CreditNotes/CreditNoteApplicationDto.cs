namespace Finance.Application.CreditNotes;

public sealed record CreditNoteApplicationDto(
    Guid Id,
    Guid InvoiceId,
    string InvoiceNumber,
    decimal Amount,
    string Note,
    DateTimeOffset AppliedAt,
    string AppliedBy,
    string AppliedByName);
