using Sales.Application.Common;
using Sales.Application.Quotations;
using Sales.Application.SalesOrders;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;

namespace Sales.Application.Common;

public static class SalesTotals
{
    public static decimal ComputeLineTotal(decimal quantity, decimal unitPrice, decimal discountPercent, decimal taxPercent)
    {
        var baseAmount = quantity * unitPrice;
        var afterDiscount = baseAmount * (1 - discountPercent / 100m);
        return Math.Round(afterDiscount * (1 + taxPercent / 100m), 2, MidpointRounding.AwayFromZero);
    }

    public static (decimal Subtotal, decimal TaxAmount, decimal TotalAmount) Compute(
        IEnumerable<(decimal Quantity, decimal UnitPrice, decimal DiscountPercent, decimal TaxPercent)> lines,
        decimal discountAmount)
    {
        decimal subtotal = 0;
        decimal tax = 0;
        foreach (var line in lines)
        {
            var baseAmount = line.Quantity * line.UnitPrice;
            var afterDiscount = baseAmount * (1 - line.DiscountPercent / 100m);
            subtotal += afterDiscount;
            tax += afterDiscount * (line.TaxPercent / 100m);
        }

        subtotal = Math.Round(subtotal, 2, MidpointRounding.AwayFromZero);
        tax = Math.Round(tax, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(Math.Max(0, subtotal + tax - discountAmount), 2, MidpointRounding.AwayFromZero);
        return (subtotal, tax, total);
    }
}

public static class SalesMappers
{
    public static AddressDto MapAddress(string? json)
    {
        var address = JsonColumn.Deserialize(json, new AddressDto("", null, "", "", "", ""));
        return address;
    }

    public static AddressDto? MapOptionalAddress(string? json) =>
        string.IsNullOrWhiteSpace(json) ? null : MapAddress(json);

    public static QuotationLineDto MapLine(QuotationLine line) =>
        new(
            line.Id,
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Description,
            line.ProductVersionId,
            line.ProductVersionLabel,
            line.Quantity,
            line.UnitPrice,
            line.DiscountPercent,
            line.TaxPercent,
            line.LineTotal,
            line.IsCustomized,
            JsonColumn.ParseElement(line.CustomizationJson),
            line.RequiresManufacturing);

    public static QuotationContactDto MapContact(QuotationContact contact) =>
        new(
            contact.Id,
            contact.Type,
            contact.Summary,
            contact.Detail,
            contact.ContactedBy,
            contact.ContactedByName,
            contact.ContactedAtUtc,
            contact.Outcome);

    public static QuotationDto MapQuotation(Quotation quotation) =>
        new(
            quotation.Id,
            quotation.Number,
            quotation.CustomerId,
            quotation.CustomerName,
            quotation.CustomerEmail,
            quotation.Status,
            quotation.Priority,
            quotation.Lines.OrderBy(l => l.SortOrder).Select(MapLine).ToList(),
            quotation.Subtotal,
            quotation.DiscountAmount,
            quotation.TaxAmount,
            quotation.TotalAmount,
            quotation.Currency,
            quotation.ValidUntil,
            quotation.PaymentStatus,
            MapAddress(quotation.BillingAddressJson),
            MapOptionalAddress(quotation.ShippingAddressJson),
            quotation.Notes,
            quotation.Terms,
            JsonColumn.Deserialize(quotation.AttachmentsJson, Array.Empty<AttachmentDto>()),
            quotation.SalesOrderId,
            quotation.Contacts.OrderByDescending(c => c.ContactedAtUtc).Select(MapContact).ToList(),
            JsonColumn.Deserialize(quotation.RevisionsJson, Array.Empty<QuotationRevisionDto>()),
            quotation.CreatedBy,
            quotation.CreatedByName,
            quotation.SentAtUtc,
            quotation.ViewedAtUtc,
            quotation.AcceptedAtUtc,
            quotation.CreatedOnUtc,
            quotation.ModifiedOnUtc);

    public static SalesOrderLineDto MapLine(SalesOrderLine line) =>
        new(
            line.Id,
            line.ProductId,
            line.ProductSku,
            line.ProductName,
            line.Description,
            line.ProductVersionId,
            line.ProductVersionLabel,
            line.Quantity,
            line.UnitPrice,
            line.DiscountPercent,
            line.TaxPercent,
            line.LineTotal,
            line.QuantityDelivered,
            line.QuantityInManufacturing,
            line.IsCustomized,
            JsonColumn.ParseElement(line.CustomizationJson),
            line.RequiresManufacturing);

    public static SalesOrderDto MapSalesOrder(SalesOrder order) =>
        new(
            order.Id,
            order.Number,
            order.CustomerId,
            order.CustomerName,
            order.CustomerEmail,
            order.QuotationId,
            order.QuotationNumber,
            order.CostingRequestId,
            order.Status,
            order.Priority,
            order.Lines.OrderBy(l => l.SortOrder).Select(MapLine).ToList(),
            order.Subtotal,
            order.DiscountAmount,
            order.TaxAmount,
            order.TotalAmount,
            order.Currency,
            order.PaymentStatus,
            MapAddress(order.BillingAddressJson),
            MapOptionalAddress(order.ShippingAddressJson),
            order.RequestedDeliveryDate,
            order.Notes,
            order.AssignedToUserId,
            order.AssignedToName,
            JsonColumn.Deserialize(order.ManufacturingJobIdsJson, Array.Empty<string>()),
            JsonColumn.Deserialize(order.DeliveryIdsJson, Array.Empty<string>()),
            order.CreatedBy,
            order.CreatedByName,
            order.ConfirmedAtUtc,
            order.CreatedOnUtc,
            order.ModifiedOnUtc);
}
