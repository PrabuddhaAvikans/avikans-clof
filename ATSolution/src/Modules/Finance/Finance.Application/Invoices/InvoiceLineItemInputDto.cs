namespace Finance.Application.Invoices;

public sealed record InvoiceLineItemInputDto(
    Guid? Id,
    Guid ProductId,
    string ProductSku,
    string ProductName,
    decimal Quantity,
    decimal UnitPrice,
    decimal TaxPercent,
    decimal? LineTotal = null);
