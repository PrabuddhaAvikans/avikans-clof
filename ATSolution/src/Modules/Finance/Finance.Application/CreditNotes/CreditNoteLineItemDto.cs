namespace Finance.Application.CreditNotes;

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
