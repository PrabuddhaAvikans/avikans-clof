namespace Finance.Api.DTOs.Requests;

public sealed record ApplyCreditNoteRequest(
    Guid InvoiceId,
    decimal Amount,
    string? Note = null,
    string? AppliedBy = null,
    string? AppliedByName = null);
