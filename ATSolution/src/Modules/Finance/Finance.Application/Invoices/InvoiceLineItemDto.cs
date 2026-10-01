namespace Finance.Application.Invoices;

public sealed record InvoiceLineItemDto(
    Guid Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal LineTotal);
