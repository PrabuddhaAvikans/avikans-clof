using Finance.Application.CreditNotes;
using Finance.Application.Invoices;
using Finance.Domain.CreditNotes;
using Finance.Domain.Invoices;

namespace Finance.Application.Common;

public static class FinanceMappers
{
    public static InvoiceDto MapInvoice(Invoice invoice) =>
        new(
            invoice.Id,
            invoice.InvoiceNumber,
            invoice.CustomerId,
            invoice.CustomerName,
            invoice.CustomerEmail,
            invoice.SalesOrderId,
            invoice.SalesOrderNumber,
            invoice.Status,
            invoice.IssueDate,
            invoice.DueDate,
            invoice.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(MapInvoiceLine)
                .ToList(),
            invoice.Subtotal,
            invoice.TaxAmount,
            invoice.TotalAmount,
            invoice.AmountPaid,
            invoice.AmountCredited,
            invoice.OutstandingAmount,
            invoice.Currency,
            invoice.Notes,
            invoice.CreatedBy,
            invoice.CreatedByName,
            invoice.CreatedOnUtc,
            invoice.ModifiedOnUtc);

    public static InvoiceLineItemDto MapInvoiceLine(InvoiceLine line) =>
        new(
            line.Id,
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Quantity,
            line.UnitPrice,
            line.TaxPercent,
            line.LineTotal);

    public static CreditNoteDto MapCreditNote(CreditNote creditNote) =>
        new(
            creditNote.Id,
            creditNote.CreditNoteNumber,
            creditNote.CustomerId,
            creditNote.CustomerName,
            creditNote.CustomerEmail,
            creditNote.InvoiceId,
            creditNote.InvoiceNumber,
            creditNote.SalesOrderId,
            creditNote.SalesOrderNumber,
            creditNote.Status,
            creditNote.Reason,
            creditNote.IssueDate,
            creditNote.LineItems
                .OrderBy(l => l.SortOrder)
                .Select(MapCreditNoteLine)
                .ToList(),
            creditNote.Subtotal,
            creditNote.TaxAmount,
            creditNote.TotalAmount,
            creditNote.AppliedAmount,
            creditNote.RemainingAmount,
            creditNote.Currency,
            creditNote.Notes,
            creditNote.Applications
                .OrderByDescending(a => a.AppliedAt)
                .Select(MapApplication)
                .ToList(),
            creditNote.CreatedBy,
            creditNote.CreatedByName,
            creditNote.IssuedBy,
            creditNote.IssuedByName,
            creditNote.CreatedOnUtc,
            creditNote.ModifiedOnUtc);

    public static CreditNoteLineItemDto MapCreditNoteLine(CreditNoteLine line) =>
        new(
            line.Id,
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Description,
            line.Quantity,
            line.UnitPrice,
            line.TaxPercent,
            line.LineTotal);

    public static CreditNoteApplicationDto MapApplication(CreditNoteApplication application) =>
        new(
            application.Id,
            application.InvoiceId,
            application.InvoiceNumber,
            application.Amount,
            application.Note,
            application.AppliedAt,
            application.AppliedBy,
            application.AppliedByName);

    public static (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) ComputeTotals(
        IEnumerable<(decimal Quantity, decimal UnitPrice, decimal TaxPercent)> lines)
    {
        decimal subtotal = 0;
        decimal taxAmount = 0;
        foreach (var line in lines)
        {
            var lineSubtotal = Round(line.Quantity * line.UnitPrice);
            var lineTax = Round(lineSubtotal * line.TaxPercent / 100m);
            subtotal += lineSubtotal;
            taxAmount += lineTax;
        }

        return (subtotal, taxAmount, Round(subtotal + taxAmount));
    }

    public static decimal ComputeLineTotal(decimal quantity, decimal unitPrice, decimal taxPercent)
    {
        var lineSubtotal = Round(quantity * unitPrice);
        var lineTax = Round(lineSubtotal * taxPercent / 100m);
        return Round(lineSubtotal + lineTax);
    }

    private static decimal Round(decimal value) =>
        Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
