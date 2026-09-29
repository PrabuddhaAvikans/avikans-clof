using ATSolution.Application;
using ATSolution.Application.Abstractions.Persistence;
using ATSolution.Application.Exceptions;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Inventory.Domain.Movements;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Microsoft.EntityFrameworkCore;
using Reporting.Application.Abstractions;
using Reporting.Application.Reports;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;

namespace Reporting.Application.Services;

public sealed class ReportService : IReportService
{
    private static readonly Dictionary<string, (string Title, ReportColumnDto[] Columns)> Definitions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["quotation-register"] = ("Quotation Register",
        [
            Col("quotationNumber", "Quotation"),
            Col("customerName", "Customer"),
            Col("status", "Status", "status"),
            Col("lineCount", "Lines", "number"),
            Col("totalAmount", "Total", "currency"),
            Col("paymentStatus", "Payment", "status"),
            Col("validUntil", "Valid until", "date"),
            Col("salesOrderId", "Sales order"),
            Col("createdByName", "Created by"),
            Col("createdAt", "Created", "date"),
        ]),
        ["sales-order-register"] = ("Sales Order Register",
        [
            Col("orderNumber", "Order"),
            Col("customerName", "Customer"),
            Col("status", "Status", "status"),
            Col("priority", "Priority", "status"),
            Col("quotationNumber", "Quotation"),
            Col("totalAmount", "Total", "currency"),
            Col("paymentStatus", "Payment", "status"),
            Col("jobCount", "Jobs", "number"),
            Col("requestedDeliveryDate", "Requested delivery", "date"),
            Col("assignedToName", "Assigned to"),
            Col("createdAt", "Created", "date"),
        ]),
        ["costing-register"] = ("Costing Request Register",
        [
            Col("requestNumber", "Request"),
            Col("customerName", "Customer"),
            Col("projectName", "Project"),
            Col("status", "Status", "status"),
            Col("riskFlag", "Risk", "status"),
            Col("coatingStatus", "Coating", "status"),
            Col("totalEstimate", "Estimate", "currency"),
            Col("proposedPrice", "Proposed price", "currency"),
            Col("marginPercent", "Margin", "percent"),
            Col("targetMargin", "Target margin", "percent"),
            Col("salesOrderNumber", "Sales order"),
            Col("slaRemaining", "SLA"),
        ]),
        ["estimation-materials"] = ("Estimation Material Requirements",
        [
            Col("requestNumber", "Request"),
            Col("customerName", "Customer"),
            Col("sku", "SKU"),
            Col("inventoryItemName", "Material"),
            Col("sourceProductName", "For product"),
            Col("quantity", "Qty", "number"),
            Col("wastePercent", "Waste", "percent"),
            Col("requiredQuantity", "Required", "number"),
            Col("unit", "Unit"),
            Col("unitCost", "Unit cost", "currency"),
            Col("totalCost", "Line cost", "currency"),
        ]),
        ["production-jobs"] = ("Production Job Register",
        [
            Col("jobNumber", "Job"),
            Col("salesOrderNumber", "Sales order"),
            Col("customerName", "Customer"),
            Col("productSku", "SKU"),
            Col("productName", "Product"),
            Col("quantity", "Qty", "number"),
            Col("status", "Status", "status"),
            Col("priority", "Priority", "status"),
            Col("progressPercent", "Progress", "percent"),
            Col("estimatedCost", "Estimated cost", "currency"),
            Col("actualCost", "Actual cost", "currency"),
            Col("plannedEndDate", "Due", "date"),
            Col("assignedToName", "Supervisor"),
        ]),
        ["quality-inspections"] = ("Quality Inspection Report",
        [
            Col("jobNumber", "Job"),
            Col("inspectionNumber", "Inspection"),
            Col("productName", "Product"),
            Col("customerName", "Customer"),
            Col("status", "Result", "status"),
            Col("inspectorName", "Inspector"),
            Col("checklistTotal", "Checks", "number"),
            Col("checklistPassed", "Passed", "number"),
            Col("passRate", "Pass rate", "percent"),
            Col("inspectedAt", "Inspected", "datetime"),
            Col("notes", "Notes"),
        ]),
        ["ready-to-ship"] = ("Ready to Ship",
        [
            Col("jobNumber", "Job"),
            Col("salesOrderNumber", "Sales order"),
            Col("customerName", "Customer"),
            Col("productName", "Product"),
            Col("quantity", "Finished", "number"),
            Col("remainingQuantity", "To ship", "number"),
            Col("shippedQuantity", "Shipped", "number"),
            Col("shipStatus", "Ship check", "status"),
            Col("actualEndDate", "Completed", "date"),
            Col("actualCost", "Actual cost", "currency"),
        ]),
        ["stock-valuation"] = ("Stock On Hand & Valuation",
        [
            Col("sku", "SKU"),
            Col("name", "Item"),
            Col("itemType", "Type", "status"),
            Col("warehouse", "Warehouse"),
            Col("location", "Location"),
            Col("quantityOnHand", "On hand", "number"),
            Col("quantityReserved", "Reserved", "number"),
            Col("quantityAvailable", "Available", "number"),
            Col("unit", "Unit"),
            Col("costPrice", "Cost", "currency"),
            Col("stockValue", "Stock value", "currency"),
            Col("stockStatus", "Stock status", "status"),
        ]),
        ["stock-movements"] = ("Stock Movement Ledger",
        [
            Col("inventoryItemSku", "SKU"),
            Col("inventoryItemName", "Item"),
            Col("type", "Type", "status"),
            Col("quantity", "Quantity", "number"),
            Col("unit", "Unit"),
            Col("referenceType", "Reference type"),
            Col("referenceId", "Reference"),
            Col("performedByName", "Performed by"),
            Col("performedAt", "When", "datetime"),
            Col("notes", "Notes"),
        ]),
        ["reprocessing-batches"] = ("Reprocessing Batch Register",
        [
            Col("batchNumber", "Batch"),
            Col("status", "Status", "status"),
            Col("inputScrapSku", "Input SKU"),
            Col("inputScrapName", "Input"),
            Col("inputQuantity", "Input qty", "number"),
            Col("inputUnitCost", "Input unit cost", "currency"),
            Col("totalProcessingCost", "Processing cost", "currency"),
            Col("recoveredQuantity", "Recovered", "number"),
            Col("processLossQuantity", "Process loss", "number"),
            Col("createdByName", "Created by"),
            Col("createdAt", "Created", "date"),
        ]),
        ["low-stock"] = ("Low Stock & Reorder",
        [
            Col("sku", "SKU"),
            Col("name", "Item"),
            Col("warehouse", "Warehouse"),
            Col("quantityAvailable", "Available", "number"),
            Col("minStock", "Min", "number"),
            Col("reorderLevel", "Reorder level", "number"),
            Col("reorderQuantity", "Reorder qty", "number"),
            Col("stockStatus", "Status", "status"),
            Col("supplier", "Supplier"),
        ]),
        ["invoice-register"] = ("Invoice Register",
        [
            Col("invoiceNumber", "Invoice"),
            Col("customerName", "Customer"),
            Col("salesOrderNumber", "Sales order"),
            Col("status", "Status", "status"),
            Col("issueDate", "Issued", "date"),
            Col("dueDate", "Due", "date"),
            Col("totalAmount", "Total", "currency"),
            Col("amountPaid", "Paid", "currency"),
            Col("amountCredited", "Credited", "currency"),
            Col("outstandingAmount", "Outstanding", "currency"),
        ]),
        ["credit-notes"] = ("Credit Note Register",
        [
            Col("creditNoteNumber", "Credit note"),
            Col("customerName", "Customer"),
            Col("invoiceNumber", "Invoice"),
            Col("status", "Status", "status"),
            Col("reason", "Reason", "status"),
            Col("totalAmount", "Total", "currency"),
            Col("appliedAmount", "Applied", "currency"),
            Col("remainingAmount", "Remaining", "currency"),
            Col("issueDate", "Issued", "date"),
        ]),
        ["delivery-register"] = ("Delivery Register",
        [
            Col("deliveryNumber", "Delivery"),
            Col("salesOrderNumber", "Sales order"),
            Col("customerName", "Customer"),
            Col("status", "Status", "status"),
            Col("priority", "Priority", "status"),
            Col("scheduledDate", "Scheduled", "date"),
            Col("dispatchedAt", "Dispatched", "datetime"),
            Col("deliveredAt", "Delivered", "datetime"),
            Col("carrier", "Carrier"),
            Col("driverName", "Driver"),
            Col("hasProof", "POD"),
        ]),
        ["customer-directory"] = ("Customer Directory",
        [
            Col("code", "Code"),
            Col("name", "Customer"),
            Col("type", "Type", "status"),
            Col("status", "Status", "status"),
            Col("email", "Email"),
            Col("phone", "Phone"),
            Col("paymentTermsDays", "Payment terms (days)", "number"),
            Col("creditLimit", "Credit limit", "currency"),
            Col("totalOrders", "Orders", "number"),
            Col("totalRevenue", "Revenue", "currency"),
        ]),
        ["product-catalog"] = ("Product Catalog",
        [
            Col("sku", "SKU"),
            Col("name", "Product"),
            Col("productType", "Type", "status"),
            Col("categoryName", "Category"),
            Col("brandName", "Brand"),
            Col("status", "Status", "status"),
            Col("versionLabel", "Version"),
            Col("costPrice", "Cost", "currency"),
            Col("basePrice", "List price", "currency"),
            Col("marginPercent", "Margin", "percent"),
            Col("leadTimeDays", "Lead time (days)", "number"),
        ]),
        ["audit-activity"] = ("Audit Activity",
        [
            Col("timestamp", "When", "datetime"),
            Col("userName", "User"),
            Col("action", "Action", "status"),
            Col("entity", "Entity"),
            Col("entityLabel", "Record"),
            Col("details", "Details"),
            Col("severity", "Severity", "status"),
        ]),
    };

    private readonly IRepository<Customer, Guid> _customers;
    private readonly IRepository<Quotation, Guid> _quotations;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<CostingRequest, Guid> _costing;
    private readonly IRepository<InventoryItem, Guid> _inventory;
    private readonly IRepository<StockMovement, Guid> _movements;
    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IRepository<DeliveryEntity, Guid> _deliveries;
    private readonly IRepository<Product, Guid> _products;

    public ReportService(
        IRepository<Customer, Guid> customers,
        IRepository<Quotation, Guid> quotations,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<CostingRequest, Guid> costing,
        IRepository<InventoryItem, Guid> inventory,
        IRepository<StockMovement, Guid> movements,
        IRepository<ManufacturingJob, Guid> jobs,
        IRepository<DeliveryEntity, Guid> deliveries,
        IRepository<Product, Guid> products)
    {
        _customers = customers;
        _quotations = quotations;
        _salesOrders = salesOrders;
        _costing = costing;
        _inventory = inventory;
        _movements = movements;
        _jobs = jobs;
        _deliveries = deliveries;
        _products = products;
    }

    public async Task<ReportDatasetDto> GetReportAsync(string reportId, CancellationToken cancellationToken = default)
    {
        if (!Definitions.TryGetValue(reportId, out var definition))
            throw new NotFoundException($"Report '{reportId}' was not found.");

        var rows = reportId.ToLowerInvariant() switch
        {
            "quotation-register" => await BuildQuotationsAsync(cancellationToken),
            "sales-order-register" => await BuildSalesOrdersAsync(cancellationToken),
            "costing-register" => await BuildCostingAsync(cancellationToken),
            "production-jobs" => await BuildJobsAsync(cancellationToken),
            "ready-to-ship" => await BuildReadyToShipAsync(cancellationToken),
            "stock-valuation" => await BuildStockValuationAsync(cancellationToken),
            "stock-movements" => await BuildStockMovementsAsync(cancellationToken),
            "low-stock" => await BuildLowStockAsync(cancellationToken),
            "delivery-register" => await BuildDeliveriesAsync(cancellationToken),
            "customer-directory" => await BuildCustomersAsync(cancellationToken),
            "product-catalog" => await BuildProductsAsync(cancellationToken),
            _ => [],
        };

        var kpis = new List<ReportKpiDto>
        {
            new("row-count", "Rows", rows.Count, "number"),
        };

        return new ReportDatasetDto(
            reportId,
            definition.Title,
            DateTimeOffset.UtcNow.ToString("O"),
            kpis,
            rows,
            definition.Columns);
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildQuotationsAsync(CancellationToken ct)
    {
        var items = await _quotations.Query().AsNoTracking()
            .Include(q => q.Lines)
            .OrderByDescending(q => q.CreatedOnUtc)
            .ToListAsync(ct);

        return items.Select(q => Row(
            q.Id.ToString(),
            ("quotationNumber", q.Number),
            ("customerName", q.CustomerName),
            ("status", q.Status),
            ("lineCount", q.Lines.Count),
            ("totalAmount", q.TotalAmount),
            ("paymentStatus", q.PaymentStatus),
            ("validUntil", q.ValidUntil.ToString("O")),
            ("salesOrderId", q.SalesOrderId?.ToString()),
            ("createdByName", q.CreatedByName),
            ("createdAt", q.CreatedOnUtc.ToString("O")))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildSalesOrdersAsync(CancellationToken ct)
    {
        var items = await _salesOrders.Query().AsNoTracking()
            .OrderByDescending(o => o.CreatedOnUtc)
            .ToListAsync(ct);

        return items.Select(o =>
        {
            var jobCount = 0;
            try
            {
                jobCount = System.Text.Json.JsonSerializer.Deserialize<List<string>>(o.ManufacturingJobIdsJson)?.Count ?? 0;
            }
            catch { /* ignore */ }

            return Row(
                o.Id.ToString(),
                ("orderNumber", o.Number),
                ("customerName", o.CustomerName),
                ("status", o.Status),
                ("priority", o.Priority),
                ("quotationNumber", o.QuotationNumber),
                ("totalAmount", o.TotalAmount),
                ("paymentStatus", o.PaymentStatus),
                ("jobCount", jobCount),
                ("requestedDeliveryDate", o.RequestedDeliveryDate?.ToString("O")),
                ("assignedToName", o.AssignedToName),
                ("createdAt", o.CreatedOnUtc.ToString("O")));
        }).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildCostingAsync(CancellationToken ct)
    {
        var items = await _costing.Query().AsNoTracking()
            .OrderByDescending(c => c.RequestedDateUtc)
            .ToListAsync(ct);

        return items.Select(c => Row(
            c.Id.ToString(),
            ("requestNumber", c.Number),
            ("customerName", c.CustomerName),
            ("projectName", c.ProjectName),
            ("status", c.Status),
            ("riskFlag", c.RiskFlag),
            ("coatingStatus", c.CoatingStatus),
            ("totalEstimate", c.TotalEstimate),
            ("proposedPrice", c.ProposedPrice),
            ("marginPercent", c.MarginPercent),
            ("targetMargin", c.TargetMargin),
            ("salesOrderNumber", c.SalesOrderNumber),
            ("slaRemaining", c.SlaRemaining))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildJobsAsync(CancellationToken ct)
    {
        var items = await _jobs.Query().AsNoTracking()
            .OrderByDescending(j => j.CreatedOnUtc)
            .ToListAsync(ct);

        return items.Select(j => Row(
            j.Id.ToString(),
            ("jobNumber", j.Number),
            ("salesOrderNumber", j.SalesOrderNumber),
            ("customerName", j.CustomerName),
            ("productSku", j.ProductSku),
            ("productName", j.ProductName),
            ("quantity", j.Quantity),
            ("status", j.Status),
            ("priority", j.Priority),
            ("progressPercent", j.OverallProgress),
            ("estimatedCost", j.EstimatedCost),
            ("actualCost", j.ActualCost),
            ("plannedEndDate", j.PlannedEnd.ToString("O")),
            ("assignedToName", j.AssignedToName))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildReadyToShipAsync(CancellationToken ct)
    {
        var items = await _jobs.Query().AsNoTracking()
            .Where(j => j.Status == ManufacturingJobStatuses.Completed)
            .OrderByDescending(j => j.ActualEndUtc)
            .ToListAsync(ct);

        return items.Select(j => Row(
            j.Id.ToString(),
            ("jobNumber", j.Number),
            ("salesOrderNumber", j.SalesOrderNumber),
            ("customerName", j.CustomerName),
            ("productName", j.ProductName),
            ("quantity", j.Quantity),
            ("remainingQuantity", j.Quantity),
            ("shippedQuantity", 0m),
            ("shipStatus", "pending"),
            ("actualEndDate", j.ActualEndUtc?.ToString("O")),
            ("actualCost", j.ActualCost))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildStockValuationAsync(CancellationToken ct)
    {
        var items = await _inventory.Query().AsNoTracking()
            .OrderBy(i => i.Sku)
            .ToListAsync(ct);

        return items.Select(i => Row(
            i.Id.ToString(),
            ("sku", i.Sku),
            ("name", i.Name),
            ("itemType", i.ItemType),
            ("warehouse", i.Warehouse),
            ("location", i.Location),
            ("quantityOnHand", i.QuantityOnHand),
            ("quantityReserved", i.QuantityReserved),
            ("quantityAvailable", i.QuantityAvailable),
            ("unit", i.Unit),
            ("costPrice", i.CostPrice),
            ("stockValue", i.QuantityOnHand * i.CostPrice),
            ("stockStatus", i.StockStatus))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildStockMovementsAsync(CancellationToken ct)
    {
        var items = await _movements.Query().AsNoTracking()
            .OrderByDescending(m => m.PerformedAtUtc)
            .Take(500)
            .ToListAsync(ct);

        return items.Select(m => Row(
            m.Id.ToString(),
            ("inventoryItemSku", m.InventoryItemSku),
            ("inventoryItemName", m.InventoryItemName),
            ("type", m.Type),
            ("quantity", m.Quantity),
            ("unit", m.Unit),
            ("referenceType", m.ReferenceType),
            ("referenceId", m.ReferenceId),
            ("performedByName", m.PerformedByName),
            ("performedAt", m.PerformedAtUtc.ToString("O")),
            ("notes", m.Notes))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildLowStockAsync(CancellationToken ct)
    {
        var items = await _inventory.Query().AsNoTracking()
            .Where(i => i.StockStatus == StockStatuses.LowStock || i.StockStatus == StockStatuses.OutOfStock)
            .OrderBy(i => i.Sku)
            .ToListAsync(ct);

        return items.Select(i => Row(
            i.Id.ToString(),
            ("sku", i.Sku),
            ("name", i.Name),
            ("warehouse", i.Warehouse),
            ("quantityAvailable", i.QuantityAvailable),
            ("minStock", i.MinStock),
            ("reorderLevel", i.ReorderLevel),
            ("reorderQuantity", i.ReorderQuantity),
            ("stockStatus", i.StockStatus),
            ("supplier", i.Supplier))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildDeliveriesAsync(CancellationToken ct)
    {
        var items = await _deliveries.Query().AsNoTracking()
            .OrderByDescending(d => d.ScheduledDate)
            .ToListAsync(ct);

        return items.Select(d => Row(
            d.Id.ToString(),
            ("deliveryNumber", d.Number),
            ("salesOrderNumber", d.SalesOrderNumber),
            ("customerName", d.CustomerName),
            ("status", d.Status),
            ("priority", d.Priority),
            ("scheduledDate", d.ScheduledDate.ToString("O")),
            ("dispatchedAt", d.DispatchedAtUtc?.ToString("O")),
            ("deliveredAt", d.DeliveredAtUtc?.ToString("O")),
            ("carrier", d.Carrier),
            ("driverName", d.DriverName),
            ("hasProof", !string.IsNullOrWhiteSpace(d.ProofOfDeliveryJson)))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildCustomersAsync(CancellationToken ct)
    {
        var items = await _customers.Query().AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return items.Select(c => Row(
            c.Id.ToString(),
            ("code", c.Code),
            ("name", c.Name),
            ("type", c.Type),
            ("status", c.Status),
            ("email", c.Email),
            ("phone", c.Phone),
            ("paymentTermsDays", c.PaymentTermsDays),
            ("creditLimit", c.CreditLimit),
            ("totalOrders", c.TotalOrders),
            ("totalRevenue", c.TotalRevenue))).ToList();
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, object?>>> BuildProductsAsync(CancellationToken ct)
    {
        var items = await _products.Query().AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Versions)
            .OrderBy(p => p.Sku)
            .ToListAsync(ct);

        return items.Select(p =>
        {
            var version = p.Versions.FirstOrDefault(v => v.Id == p.CurrentVersionId)
                          ?? p.Versions.OrderByDescending(v => v.VersionNumber).FirstOrDefault();
            return Row(
                p.Id.ToString(),
                ("sku", p.Sku),
                ("name", p.Name),
                ("productType", p.ProductType),
                ("categoryName", p.Category?.Name),
                ("brandName", p.Brand?.Name),
                ("status", p.Status),
                ("versionLabel", version?.Label),
                ("costPrice", version?.CostPrice ?? 0m),
                ("basePrice", version?.SellingPrice ?? 0m),
                ("marginPercent", version?.MarginPercent ?? 0m),
                ("leadTimeDays", version?.LeadTimeDays ?? 0));
        }).ToList();
    }

    private static ReportColumnDto Col(string key, string label, string type = "text", string? align = null) =>
        new(key, label, type, align ?? (type is "number" or "currency" or "percent" ? "right" : "left"));

    private static IReadOnlyDictionary<string, object?> Row(string id, params (string Key, object? Value)[] fields)
    {
        var dict = new Dictionary<string, object?> { ["id"] = id };
        foreach (var (key, value) in fields)
            dict[key] = value;
        return dict;
    }
}
