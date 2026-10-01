using Sales.Application.Quotations;
using Sales.Application.SalesOrders;
using Sales.Domain.Common;
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
        var nets = new List<(decimal Net, decimal TaxPercent)>();
        decimal subtotalRaw = 0;
        foreach (var line in lines)
        {
            var net = line.Quantity * line.UnitPrice * (1 - line.DiscountPercent / 100m);
            nets.Add((net, line.TaxPercent));
            subtotalRaw += net;
        }

        var subtotal = Math.Round(subtotalRaw, 2, MidpointRounding.AwayFromZero);
        var discount = Math.Min(Math.Max(discountAmount, 0), subtotal);
        decimal taxableTax = 0;
        if (subtotalRaw > 0)
        {
            foreach (var line in nets)
            {
                var share = line.Net / subtotalRaw;
                var lineTaxable = line.Net - (discount * share);
                taxableTax += lineTaxable * (line.TaxPercent / 100m);
            }
        }

        var tax = Math.Round(taxableTax, 2, MidpointRounding.AwayFromZero);
        var total = Math.Round(Math.Max(0, subtotal - discount + tax), 2, MidpointRounding.AwayFromZero);
        return (subtotal, tax, total);
    }

    public static decimal MarginPercent(decimal sellingPrice, decimal productionCost)
    {
        if (sellingPrice <= 0) return 0;
        return Math.Round((sellingPrice - productionCost) / sellingPrice * 100m, 2, MidpointRounding.AwayFromZero);
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
            ResolveRejectionReason(quotation),
            ResolveRejectedAt(quotation),
            quotation.CreatedOnUtc,
            quotation.ModifiedOnUtc);

    private static string? ResolveRejectionReason(Quotation quotation)
    {
        if (quotation.Status != QuotationStatuses.Rejected)
            return null;

        return quotation.Contacts
            .Where(contact =>
                string.Equals(contact.Outcome, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contact.Summary, "Quotation rejected", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contact => contact.ContactedAtUtc)
            .Select(contact => contact.Detail ?? contact.Summary)
            .FirstOrDefault(reason => !string.IsNullOrWhiteSpace(reason));
    }

    private static DateTimeOffset? ResolveRejectedAt(Quotation quotation)
    {
        if (quotation.Status != QuotationStatuses.Rejected)
            return null;

        return quotation.Contacts
            .Where(contact =>
                string.Equals(contact.Outcome, "Rejected", StringComparison.OrdinalIgnoreCase)
                || string.Equals(contact.Summary, "Quotation rejected", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(contact => contact.ContactedAtUtc)
            .Select(contact => (DateTimeOffset?)contact.ContactedAtUtc)
            .FirstOrDefault();
    }

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
