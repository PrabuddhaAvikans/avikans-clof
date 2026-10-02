using System.Text.Json;
using System.Text.Json.Serialization;
using ATSolution.Application.Abstractions.Persistence;
using Audit.Domain.AuditLogs;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Delivery.Domain.Common;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
using Manufacturing.Domain.Tasks;
using Microsoft.EntityFrameworkCore;
using Reporting.Application.Abstractions;
using Reporting.Application.Dashboard;
using Sales.Domain.Common;
using Sales.Domain.Costing;
using Sales.Domain.Quotations;
using Sales.Domain.SalesOrders;
using DeliveryEntity = Delivery.Domain.Deliveries.Delivery;

namespace Reporting.Application.Services;

public sealed class DashboardService : IDashboardService
{
    private static readonly JsonSerializerOptions AuditJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IRepository<Customer, Guid> _customers;
    private readonly IRepository<Quotation, Guid> _quotations;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<CostingRequest, Guid> _costing;
    private readonly IRepository<InventoryItem, Guid> _inventory;
    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IRepository<ManufacturingTask, Guid> _tasks;
    private readonly IRepository<DeliveryEntity, Guid> _deliveries;
    private readonly IRepository<Product, Guid> _products;
    private readonly IRepository<AuditLogEntry, Guid> _auditLogs;

    public DashboardService(
        IRepository<Customer, Guid> customers,
        IRepository<Quotation, Guid> quotations,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<CostingRequest, Guid> costing,
        IRepository<InventoryItem, Guid> inventory,
        IRepository<ManufacturingJob, Guid> jobs,
        IRepository<ManufacturingTask, Guid> tasks,
        IRepository<DeliveryEntity, Guid> deliveries,
        IRepository<Product, Guid> products,
        IRepository<AuditLogEntry, Guid> auditLogs)
    {
        _customers = customers;
        _quotations = quotations;
        _salesOrders = salesOrders;
        _costing = costing;
        _inventory = inventory;
        _jobs = jobs;
        _tasks = tasks;
        _deliveries = deliveries;
        _products = products;
        _auditLogs = auditLogs;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        var customers = _customers.Query().AsNoTracking();
        var quotations = _quotations.Query().AsNoTracking();
        var orders = _salesOrders.Query().AsNoTracking();
        var costing = _costing.Query().AsNoTracking();
        var inventory = _inventory.Query().AsNoTracking();
        var jobs = _jobs.Query().AsNoTracking();
        var deliveries = _deliveries.Query().AsNoTracking();
        var products = _products.Query().AsNoTracking();

        var totalCustomers = await customers.CountAsync(cancellationToken);
        var activeQuotations = await quotations.CountAsync(
            q => q.Status != QuotationStatuses.Converted
                 && q.Status != QuotationStatuses.Expired
                 && q.Status != QuotationStatuses.Rejected,
            cancellationToken);
        var pendingApprovals = await costing.CountAsync(
            c => c.Status == CostingRequestStatuses.InReview, cancellationToken);
        var pendingEstimations = await costing.CountAsync(
            c => c.Status == CostingRequestStatuses.Pending, cancellationToken);
        var confirmedSalesOrders = await orders.CountAsync(
            o => o.Status == SalesOrderStatuses.Confirmed, cancellationToken);
        var openSalesOrders = await orders.CountAsync(
            o => o.Status != SalesOrderStatuses.Completed
                 && o.Status != SalesOrderStatuses.Cancelled
                 && o.Status != SalesOrderStatuses.Delivered,
            cancellationToken);
        var manufacturingJobsInProgress = await jobs.CountAsync(
            j => j.Status == ManufacturingJobStatuses.InProgress, cancellationToken);
        var delayedJobs = await jobs.CountAsync(
            j => j.PlannedEnd < now
                 && j.Status != ManufacturingJobStatuses.Completed
                 && j.Status != ManufacturingJobStatuses.Cancelled,
            cancellationToken);
        var qualityCheckJobs = await jobs.CountAsync(
            j => j.Status == ManufacturingJobStatuses.QualityCheck, cancellationToken);
        var readyToShip = await jobs.CountAsync(
            j => j.Status == ManufacturingJobStatuses.Completed, cancellationToken);
        var deliveriesDueToday = await deliveries.CountAsync(
            d => d.ScheduledDate >= todayStart && d.ScheduledDate < todayEnd
                 && d.Status != DeliveryStatuses.Delivered
                 && d.Status != DeliveryStatuses.Cancelled,
            cancellationToken);
        var deliveriesInTransit = await deliveries.CountAsync(
            d => d.Status == DeliveryStatuses.InTransit || d.Status == DeliveryStatuses.Dispatched,
            cancellationToken);
        var upcomingDeliveryCount = await deliveries.CountAsync(
            d => d.ScheduledDate >= todayStart
                 && d.Status != DeliveryStatuses.Delivered
                 && d.Status != DeliveryStatuses.Cancelled,
            cancellationToken);
        var lowStockItems = await inventory.CountAsync(
            i => i.StockStatus == StockStatuses.LowStock || i.StockStatus == StockStatuses.OutOfStock,
            cancellationToken);
        var productCount = await products.CountAsync(cancellationToken);

        var months = BuildMonthWindow(now);
        var windowStart = months[0];

        var quotationAmounts = await quotations
            .Where(q => q.CreatedOnUtc >= windowStart)
            .Select(q => new { q.CreatedOnUtc, q.TotalAmount })
            .ToListAsync(cancellationToken);
        var orderAmounts = await orders
            .Where(o => o.Status != SalesOrderStatuses.Cancelled)
            .Where(o => (o.ConfirmedAtUtc ?? o.CreatedOnUtc) >= windowStart)
            .Select(o => new { Date = o.ConfirmedAtUtc ?? o.CreatedOnUtc, o.TotalAmount, o.Status })
            .ToListAsync(cancellationToken);

        var monthlyQuotationValue = MonthValueSeries(
            months,
            quotationAmounts.Select(q => (q.CreatedOnUtc, q.TotalAmount)));
        var revenueByMonth = MonthValueSeries(
            months,
            orderAmounts
                .Where(o => o.Status != SalesOrderStatuses.Draft)
                .Select(o => (o.Date, o.TotalAmount)));
        var ordersByMonth = MonthCountSeries(
            months,
            orderAmounts.Select(o => o.Date));

        var monthlySalesValue = revenueByMonth.Count > 0 ? revenueByMonth[^1].Value : 0m;
        var previousSalesValue = revenueByMonth.Count > 1 ? revenueByMonth[^2].Value : 0m;
        var ordersThisMonth = ordersByMonth.Count > 0 ? ordersByMonth[^1].Value : 0m;
        var ordersLastMonth = ordersByMonth.Count > 1 ? ordersByMonth[^2].Value : 0m;

        var openJobs = await jobs.CountAsync(
            j => j.Status != ManufacturingJobStatuses.Completed
                 && j.Status != ManufacturingJobStatuses.Cancelled,
            cancellationToken);
        var productionCapacityPercent = openJobs == 0
            ? 0
            : Math.Round(100m * manufacturingJobsInProgress / openJobs, 0);

        var topProductRows = await orders
            .Where(o => o.Status != SalesOrderStatuses.Cancelled && o.Status != SalesOrderStatuses.Draft)
            .SelectMany(o => o.Lines)
            .GroupBy(l => l.ProductName != "" ? l.ProductName : l.ProductSku)
            .Select(g => new { Label = g.Key, Value = g.Sum(l => l.Quantity) })
            .OrderByDescending(x => x.Value)
            .Take(5)
            .ToListAsync(cancellationToken);
        var topProducts = topProductRows
            .Select(row => new ChartDataPointDto(TruncateLabel(row.Label), row.Value))
            .ToList();

        var quotationStatusRows = await quotations
            .GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        var orderStatusRows = await orders
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        var manufacturingStatusRows = await jobs
            .GroupBy(j => j.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        var deliveryStatusRows = await deliveries
            .GroupBy(d => d.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);
        var inventoryStatusRows = await inventory
            .GroupBy(i => i.StockStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(cancellationToken);

        var recentQuotations = await quotations
            .OrderByDescending(q => q.ModifiedOnUtc)
            .Take(5)
            .Select(q => new
            {
                q.Id,
                q.Number,
                q.Status,
                q.TotalAmount,
                q.ModifiedOnUtc,
                q.CustomerName,
                Title = q.Lines.OrderBy(l => l.SortOrder).Select(l => l.ProductName).FirstOrDefault() ?? "Quotation",
            })
            .ToListAsync(cancellationToken);

        var recentlyApprovedOrders = await orders
            .Where(o => o.Status != SalesOrderStatuses.Draft && o.Status != SalesOrderStatuses.Cancelled)
            .OrderByDescending(o => o.ConfirmedAtUtc ?? o.ModifiedOnUtc)
            .Take(5)
            .Select(o => new
            {
                o.Id,
                o.Number,
                o.Status,
                o.TotalAmount,
                Date = o.ConfirmedAtUtc ?? o.ModifiedOnUtc,
                o.CustomerName,
                Title = o.Lines.OrderBy(l => l.SortOrder).Select(l => l.ProductName).FirstOrDefault() ?? "Sales order",
            })
            .ToListAsync(cancellationToken);

        string[] attentionJobStatuses =
        [
            ManufacturingJobStatuses.OnHold,
            ManufacturingJobStatuses.Rework,
            ManufacturingJobStatuses.QualityCheck,
            ManufacturingJobStatuses.MaterialsPending,
        ];
        var jobsRequiringAttention = await jobs
            .Where(j => j.Status != ManufacturingJobStatuses.Completed
                        && j.Status != ManufacturingJobStatuses.Cancelled)
            .Where(j => attentionJobStatuses.Contains(j.Status) || j.PlannedEnd < now)
            .OrderBy(j => j.PlannedEnd)
            .Take(6)
            .Select(j => new
            {
                j.Id,
                j.Number,
                j.ProductName,
                j.Status,
                j.PlannedEnd,
                j.CustomerName,
                j.Priority,
            })
            .ToListAsync(cancellationToken);

        var upcomingDeliveries = await deliveries
            .Where(d => d.Status != DeliveryStatuses.Delivered
                        && d.Status != DeliveryStatuses.Cancelled
                        && d.Status != DeliveryStatuses.Failed
                        && d.Status != DeliveryStatuses.Returned)
            .OrderBy(d => d.ScheduledDate)
            .Take(5)
            .Select(d => new
            {
                d.Id,
                d.Number,
                d.SalesOrderNumber,
                d.Status,
                d.ScheduledDate,
                d.CustomerName,
            })
            .ToListAsync(cancellationToken);

        var pendingCostingRows = await costing
            .Where(c => c.Status == CostingRequestStatuses.Pending
                        || c.Status == CostingRequestStatuses.InReview
                        || c.Status == CostingRequestStatuses.ChangesRequested)
            .OrderByDescending(c => c.RequestedDateUtc)
            .Take(5)
            .Select(c => new
            {
                c.Id,
                c.Number,
                c.ProjectName,
                c.Status,
                c.ProposedPrice,
                c.RequestedDateUtc,
                c.CustomerName,
                c.SalesOrderId,
            })
            .ToListAsync(cancellationToken);

        var lowStockRows = await inventory
            .Where(i => i.StockStatus == StockStatuses.LowStock || i.StockStatus == StockStatuses.OutOfStock)
            .OrderBy(i => i.QuantityOnHand - i.QuantityReserved)
            .Take(6)
            .Select(i => new
            {
                i.Id,
                i.Sku,
                i.Name,
                i.StockStatus,
                i.ModifiedOnUtc,
                Available = i.QuantityOnHand - i.QuantityReserved,
                i.Unit,
            })
            .ToListAsync(cancellationToken);

        return new DashboardSummaryDto(
            GeneratedAt: now,
            PeriodLabel: now.ToString("MMMM yyyy"),
            TotalCustomers: totalCustomers,
            ActiveQuotations: activeQuotations,
            PendingApprovals: pendingApprovals,
            PendingEstimations: pendingEstimations,
            ConfirmedSalesOrders: confirmedSalesOrders,
            OpenSalesOrders: openSalesOrders,
            ManufacturingJobsInProgress: manufacturingJobsInProgress,
            DelayedJobs: delayedJobs,
            QualityCheckJobs: qualityCheckJobs,
            ReadyToShip: readyToShip,
            DeliveriesDueToday: deliveriesDueToday,
            DeliveriesInTransit: deliveriesInTransit,
            UpcomingDeliveryCount: upcomingDeliveryCount,
            LowStockItems: lowStockItems,
            ReprocessingInProgress: 0,
            ProductCount: productCount,
            MonthlySalesValue: monthlySalesValue,
            RevenueGrowthPercent: GrowthPercent(monthlySalesValue, previousSalesValue),
            OrdersGrowthPercent: GrowthPercent(ordersThisMonth, ordersLastMonth),
            ProductionCapacityPercent: productionCapacityPercent,
            MonthlyQuotationValue: monthlyQuotationValue,
            RevenueByMonth: revenueByMonth,
            OrdersByMonth: ordersByMonth,
            QuotationConversion: ToStatusChart(quotationStatusRows.Select(x => (x.Status, x.Count))),
            OrdersByStatus: ToStatusChart(orderStatusRows.Select(x => (x.Status, x.Count))),
            ManufacturingByStatus: ToStatusChart(manufacturingStatusRows.Select(x => (x.Status, x.Count))),
            DeliveriesByStatus: ToStatusChart(deliveryStatusRows.Select(x => (x.Status, x.Count))),
            InventoryByStatus: ToStatusChart(inventoryStatusRows.Select(x => (x.Status, x.Count))),
            TopProducts: topProducts,
            RecentQuotations: recentQuotations
                .Select(q => new DashboardTableRowDto(
                    q.Id.ToString(),
                    q.Number,
                    q.Title,
                    q.Status,
                    q.TotalAmount,
                    q.ModifiedOnUtc.ToString("O"),
                    q.CustomerName,
                    Href: $"/quotations/{q.Id}"))
                .ToList(),
            RecentlyApprovedOrders: recentlyApprovedOrders
                .Select(o => new DashboardTableRowDto(
                    o.Id.ToString(),
                    o.Number,
                    o.Title,
                    o.Status,
                    o.TotalAmount,
                    o.Date.ToString("O"),
                    o.CustomerName,
                    Href: $"/sales-orders/{o.Id}"))
                .ToList(),
            JobsRequiringAttention: jobsRequiringAttention
                .Select(j => new DashboardTableRowDto(
                    j.Id.ToString(),
                    j.Number,
                    j.ProductName,
                    j.Status,
                    Amount: null,
                    Date: j.PlannedEnd.ToString("O"),
                    Customer: j.CustomerName,
                    Priority: j.Priority,
                    Href: $"/manufacturing/jobs/{j.Id}"))
                .ToList(),
            UpcomingDeliveries: upcomingDeliveries
                .Select(d => new DashboardTableRowDto(
                    d.Id.ToString(),
                    d.Number,
                    d.SalesOrderNumber,
                    d.Status,
                    Amount: null,
                    Date: d.ScheduledDate.ToString("O"),
                    Customer: d.CustomerName,
                    Href: $"/deliveries/{d.Id}"))
                .ToList(),
            PendingCosting: pendingCostingRows
                .Select(c => new DashboardTableRowDto(
                    c.Id.ToString(),
                    c.Number,
                    c.ProjectName,
                    c.Status,
                    c.ProposedPrice,
                    c.RequestedDateUtc.ToString("O"),
                    c.CustomerName,
                    Href: $"/costing/approval?salesOrderId={c.SalesOrderId}"))
                .ToList(),
            LowStockRows: lowStockRows
                .Select(i => new DashboardTableRowDto(
                    i.Id.ToString(),
                    i.Sku,
                    i.Name,
                    i.StockStatus,
                    Amount: null,
                    Date: i.ModifiedOnUtc.ToString("O"),
                    Customer: $"{i.Available:0.##} {i.Unit} available",
                    Href: $"/inventory/{i.Id}"))
                .ToList(),
            RecentActivity: await GetRecentActivityAsync(cancellationToken),
            Notifications: Array.Empty<DashboardNotificationPreviewDto>());
    }

    private async Task<IReadOnlyList<DashboardActivityItemDto>> GetRecentActivityAsync(
        CancellationToken cancellationToken)
    {
        var entries = await _auditLogs.Query()
            .AsNoTracking()
            .OrderByDescending(entry => entry.Timestamp)
            .Take(24)
            .ToListAsync(cancellationToken);

        return entries
            .Select(entry => new DashboardActivityItemDto(
                Id: entry.Id.ToString(),
                Description: entry.Details,
                Timestamp: entry.Timestamp,
                Type: entry.Entity,
                User: entry.UserName,
                Href: ResolveActivityHref(entry.Entity, entry.EntityId),
                Action: entry.Action,
                EntityLabel: string.IsNullOrWhiteSpace(entry.EntityLabel) ? entry.Entity : entry.EntityLabel,
                Severity: entry.Severity,
                EntityId: entry.EntityId,
                Changes: ParseActivityChanges(entry.ChangesJson)))
            .ToList();
    }

    private static IReadOnlyList<DashboardActivityChangeDto>? ParseActivityChanges(string? changesJson)
    {
        if (string.IsNullOrWhiteSpace(changesJson)) return null;

        try
        {
            var changes = JsonSerializer.Deserialize<List<DashboardActivityChangeDto>>(
                changesJson,
                AuditJsonOptions);
            return changes is { Count: > 0 } ? changes : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ResolveActivityHref(string entity, string entityId)
    {
        if (string.IsNullOrWhiteSpace(entityId)) return null;

        return entity switch
        {
            "User" => "/admin/users",
            "Role" => "/admin/roles",
            "RoleGroup" => "/admin/role-groups",
            "Quotation" => $"/quotations/{entityId}",
            "SalesOrder" => $"/sales-orders/{entityId}",
            "Product" => $"/products/{entityId}",
            "Customer" => $"/customers/{entityId}",
            "InventoryItem" => $"/inventory/{entityId}",
            "ManufacturingJob" => $"/manufacturing/jobs/{entityId}",
            "Delivery" => $"/deliveries/{entityId}",
            "Settings" => "/configuration/settings",
            _ => null,
        };
    }

    public async Task<OrderFlowOverviewDto> GetOrderFlowAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var expireSoonEnd = todayEnd.AddDays(2);

        var quotations = _quotations.Query().AsNoTracking();
        var orders = _salesOrders.Query().AsNoTracking();
        var costing = _costing.Query().AsNoTracking();
        var jobs = _jobs.Query().AsNoTracking();
        var tasks = _tasks.Query().AsNoTracking();
        var deliveries = _deliveries.Query().AsNoTracking();

        var quotationStage = await BuildQuotationStageAsync(quotations, now, expireSoonEnd, cancellationToken);
        var salesOrderStage = await BuildSalesOrderStageAsync(orders, cancellationToken);
        var estimationStage = await BuildEstimationStageAsync(costing, cancellationToken);
        var costingStage = await BuildCostingStageAsync(costing, monthStart, cancellationToken);
        var productionStage = await BuildProductionStageAsync(jobs, tasks, now, todayStart, todayEnd, cancellationToken);
        var deliveryStage = await BuildDeliveryStageAsync(deliveries, todayStart, todayEnd, cancellationToken);
        var completedStage = await BuildCompletedStageAsync(orders, todayStart, todayEnd, monthStart, cancellationToken);

        return new OrderFlowOverviewDto(
            GeneratedAt: now,
            Quotation: quotationStage,
            SalesOrder: salesOrderStage,
            Estimation: estimationStage,
            Costing: costingStage,
            Production: productionStage,
            Delivery: deliveryStage,
            Completed: completedStage);
    }

    private static async Task<OrderFlowStageDto> BuildQuotationStageAsync(
        IQueryable<Quotation> quotations,
        DateTimeOffset now,
        DateTimeOffset expireSoonEnd,
        CancellationToken cancellationToken)
    {
        var active = quotations.Where(q =>
            q.Status != QuotationStatuses.Converted
            && q.Status != QuotationStatuses.Expired
            && q.Status != QuotationStatuses.Rejected);

        var total = await active.CountAsync(cancellationToken);
        var draft = await active.CountAsync(
            q => q.Status == QuotationStatuses.Draft || q.Status == QuotationStatuses.ReadyToSend,
            cancellationToken);
        var awaitingCustomer = await active.CountAsync(
            q => q.Status == QuotationStatuses.Sent || q.Status == QuotationStatuses.Viewed,
            cancellationToken);
        var feedback = await active.CountAsync(
            q => q.Status == QuotationStatuses.CustomerFeedback
                 || q.Status == QuotationStatuses.RevisionRequired,
            cancellationToken);
        var accepted = await active.CountAsync(
            q => q.Status == QuotationStatuses.Accepted || q.Status == QuotationStatuses.Revised,
            cancellationToken);
        var expiredOpen = await active.CountAsync(q => q.ValidUntil < now, cancellationToken);
        var expiringSoon = await active.CountAsync(
            q => q.ValidUntil >= now && q.ValidUntil < expireSoonEnd,
            cancellationToken);

        var attention = expiredOpen + feedback;
        var severity = expiredOpen > 0
            ? OrderFlowSeverities.Critical
            : attention > 0
                ? OrderFlowSeverities.Warning
                : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (expiredOpen > 0)
            messages.Add($"{expiredOpen} quotation{(expiredOpen == 1 ? "" : "s")} past validity date");
        if (expiringSoon > 0)
            messages.Add($"{expiringSoon} quotation{(expiringSoon == 1 ? "" : "s")} expire within 2 days");
        if (feedback > 0)
            messages.Add($"{feedback} awaiting customer feedback or revision");
        if (awaitingCustomer > 0)
            messages.Add($"{awaitingCustomer} waiting on customer response");
        if (messages.Count == 0)
            messages.Add(total == 0 ? "No active quotations" : "Quotations are up to date");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Quotation,
            Title: "Quotation",
            SummaryText: attention > 0
                ? $"{attention} need attention"
                : total == 0 ? "No active quotations" : "Pipeline healthy",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("draft", "Draft", draft, OrderFlowSeverities.Pending),
                new OrderFlowStatDto("awaiting_customer", "Awaiting customer", awaitingCustomer, OrderFlowSeverities.Info),
                new OrderFlowStatDto("feedback", "Feedback / revision", feedback, OrderFlowSeverities.Warning),
                new OrderFlowStatDto("accepted", "Accepted", accepted, OrderFlowSeverities.Success),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildSalesOrderStageAsync(
        IQueryable<SalesOrder> orders,
        CancellationToken cancellationToken)
    {
        var open = orders.Where(o =>
            o.Status != SalesOrderStatuses.Completed
            && o.Status != SalesOrderStatuses.Cancelled
            && o.Status != SalesOrderStatuses.Delivered);

        var total = await open.CountAsync(cancellationToken);
        var draft = await open.CountAsync(o => o.Status == SalesOrderStatuses.Draft, cancellationToken);
        var pendingReview = await open.CountAsync(
            o => o.Status == SalesOrderStatuses.PendingReview, cancellationToken);
        var submitted = await open.CountAsync(
            o => o.Status == SalesOrderStatuses.Submitted, cancellationToken);
        var confirmed = await open.CountAsync(
            o => o.Status == SalesOrderStatuses.Confirmed
                 || o.Status == SalesOrderStatuses.InManufacturing
                 || o.Status == SalesOrderStatuses.ReadyForDelivery
                 || o.Status == SalesOrderStatuses.PartiallyDelivered,
            cancellationToken);

        var attention = pendingReview;
        var severity = attention > 0 ? OrderFlowSeverities.Warning : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (pendingReview > 0)
            messages.Add($"{pendingReview} sales order{(pendingReview == 1 ? "" : "s")} pending review");
        if (draft > 0)
            messages.Add($"{draft} draft order{(draft == 1 ? "" : "s")}");
        if (submitted > 0)
            messages.Add($"{submitted} submitted and waiting confirmation");
        if (messages.Count == 0)
            messages.Add(total == 0 ? "No open sales orders" : "Sales orders progressing normally");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.SalesOrder,
            Title: "Sales Order",
            SummaryText: attention > 0
                ? $"{attention} pending review"
                : total == 0 ? "No open sales orders" : "Orders in progress",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("draft", "New", draft, OrderFlowSeverities.Pending),
                new OrderFlowStatDto("pending_review", "Pending review", pendingReview, OrderFlowSeverities.Warning),
                new OrderFlowStatDto("submitted", "Submitted", submitted, OrderFlowSeverities.Info),
                new OrderFlowStatDto("confirmed", "Confirmed+", confirmed, OrderFlowSeverities.Success),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildEstimationStageAsync(
        IQueryable<CostingRequest> costing,
        CancellationToken cancellationToken)
    {
        var pendingCoating = costing.Where(c =>
            c.CoatingStatus == CoatingStatuses.Pending
            && c.Status != CostingRequestStatuses.Rejected
            && c.Status != CostingRequestStatuses.Approved);

        var total = await pendingCoating.CountAsync(cancellationToken);
        var changesRequested = await pendingCoating.CountAsync(
            c => c.Status == CostingRequestStatuses.ChangesRequested, cancellationToken);
        var fresh = total - changesRequested;

        var attention = total;
        var severity = changesRequested > 0
            ? OrderFlowSeverities.Warning
            : attention > 0
                ? OrderFlowSeverities.Pending
                : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (fresh > 0)
            messages.Add($"{fresh} estimation{(fresh == 1 ? "" : "s")} pending completion");
        if (changesRequested > 0)
            messages.Add($"{changesRequested} returned for estimation changes");
        if (messages.Count == 0)
            messages.Add("No estimations waiting");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Estimation,
            Title: "Estimation",
            SummaryText: attention > 0
                ? $"{attention} pending estimation"
                : "No pending estimations",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("pending", "Pending", fresh, OrderFlowSeverities.Pending),
                new OrderFlowStatDto("changes_requested", "Changes requested", changesRequested, OrderFlowSeverities.Warning),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildCostingStageAsync(
        IQueryable<CostingRequest> costing,
        DateTimeOffset monthStart,
        CancellationToken cancellationToken)
    {
        var active = costing.Where(c =>
            c.Status == CostingRequestStatuses.Pending
            || c.Status == CostingRequestStatuses.InReview
            || c.Status == CostingRequestStatuses.ChangesRequested);

        var total = await active.CountAsync(cancellationToken);
        var waitingApproval = await active.CountAsync(
            c => c.Status == CostingRequestStatuses.InReview, cancellationToken);
        var pending = await active.CountAsync(
            c => c.Status == CostingRequestStatuses.Pending, cancellationToken);
        var changesRequested = await active.CountAsync(
            c => c.Status == CostingRequestStatuses.ChangesRequested, cancellationToken);
        var rejected = await costing.CountAsync(
            c => c.Status == CostingRequestStatuses.Rejected && c.ModifiedOnUtc >= monthStart,
            cancellationToken);

        var attention = waitingApproval + changesRequested;
        var severity = attention > 0 ? OrderFlowSeverities.Warning : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (waitingApproval > 0)
            messages.Add($"{waitingApproval} costing record{(waitingApproval == 1 ? "" : "s")} waiting for approval");
        if (changesRequested > 0)
            messages.Add($"{changesRequested} costing change{(changesRequested == 1 ? "" : "s")} requested");
        if (pending > 0)
            messages.Add($"{pending} costing request{(pending == 1 ? "" : "s")} not yet in review");
        if (rejected > 0)
            messages.Add($"{rejected} costing request{(rejected == 1 ? "" : "s")} rejected this month");
        if (messages.Count == 0)
            messages.Add(total == 0 ? "No active costing work" : "Costing queue clear");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Costing,
            Title: "Costing",
            SummaryText: waitingApproval > 0
                ? $"{waitingApproval} waiting approval"
                : total == 0 ? "No active costing" : "Costing in progress",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("waiting_approval", "Waiting approval", waitingApproval, OrderFlowSeverities.Warning),
                new OrderFlowStatDto("pending", "Pending", pending, OrderFlowSeverities.Pending),
                new OrderFlowStatDto("changes_requested", "Changes requested", changesRequested, OrderFlowSeverities.Warning),
                new OrderFlowStatDto("rejected", "Rejected (month)", rejected, OrderFlowSeverities.Critical),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildProductionStageAsync(
        IQueryable<ManufacturingJob> jobs,
        IQueryable<ManufacturingTask> tasks,
        DateTimeOffset now,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        CancellationToken cancellationToken)
    {
        var active = jobs.Where(j =>
            j.Status != ManufacturingJobStatuses.Completed
            && j.Status != ManufacturingJobStatuses.Cancelled);

        var total = await active.CountAsync(cancellationToken);
        var inProgress = await active.CountAsync(
            j => j.Status == ManufacturingJobStatuses.InProgress, cancellationToken);
        var onHold = await active.CountAsync(
            j => j.Status == ManufacturingJobStatuses.OnHold
                 || j.Status == ManufacturingJobStatuses.Rework
                 || j.Status == ManufacturingJobStatuses.MaterialsPending,
            cancellationToken);
        var qualityCheck = await active.CountAsync(
            j => j.Status == ManufacturingJobStatuses.QualityCheck, cancellationToken);
        var delayed = await active.CountAsync(j => j.PlannedEnd < now, cancellationToken);
        var completedToday = await jobs.CountAsync(
            j => j.Status == ManufacturingJobStatuses.Completed
                 && j.ActualEndUtc >= todayStart
                 && j.ActualEndUtc < todayEnd,
            cancellationToken);

        var blockedJobIds = await tasks
            .Where(t => t.Status == ManufacturingTaskStatuses.Blocked)
            .Select(t => t.JobId)
            .Distinct()
            .CountAsync(cancellationToken);

        var attention = delayed + onHold + blockedJobIds;
        var severity = delayed > 0 || blockedJobIds > 0
            ? OrderFlowSeverities.Critical
            : onHold > 0 || qualityCheck > 0
                ? OrderFlowSeverities.Warning
                : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (delayed > 0)
            messages.Add($"{delayed} production job{(delayed == 1 ? "" : "s")} behind schedule");
        if (blockedJobIds > 0)
            messages.Add($"{blockedJobIds} job{(blockedJobIds == 1 ? "" : "s")} with blocked operations");
        if (onHold > 0)
            messages.Add($"{onHold} job{(onHold == 1 ? "" : "s")} on hold / rework / materials pending");
        if (qualityCheck > 0)
            messages.Add($"{qualityCheck} job{(qualityCheck == 1 ? "" : "s")} in quality check");
        if (completedToday > 0)
            messages.Add($"{completedToday} completed today");
        if (messages.Count == 0)
            messages.Add(total == 0 ? "No active production jobs" : "Production running smoothly");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Production,
            Title: "Production",
            SummaryText: delayed > 0
                ? $"{delayed} delayed"
                : total == 0 ? "No active jobs" : $"{inProgress} in progress",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("in_progress", "In progress", inProgress, OrderFlowSeverities.Info),
                new OrderFlowStatDto("completed_today", "Completed today", completedToday, OrderFlowSeverities.Success),
                new OrderFlowStatDto("delayed", "Delayed", delayed, OrderFlowSeverities.Critical),
                new OrderFlowStatDto("blocked", "Blocked ops", blockedJobIds, OrderFlowSeverities.Critical),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildDeliveryStageAsync(
        IQueryable<DeliveryEntity> deliveries,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        CancellationToken cancellationToken)
    {
        var active = deliveries.Where(d =>
            d.Status != DeliveryStatuses.Delivered
            && d.Status != DeliveryStatuses.Cancelled);

        var total = await active.CountAsync(cancellationToken);
        var ready = await active.CountAsync(
            d => d.Status == DeliveryStatuses.ReadyForDispatch, cancellationToken);
        var scheduled = await active.CountAsync(
            d => d.Status == DeliveryStatuses.Planned, cancellationToken);
        var inTransit = await active.CountAsync(
            d => d.Status == DeliveryStatuses.Dispatched
                 || d.Status == DeliveryStatuses.InTransit
                 || d.Status == DeliveryStatuses.PartiallyDelivered,
            cancellationToken);
        var delayed = await active.CountAsync(d => d.ScheduledDate < todayStart, cancellationToken);
        var failed = await active.CountAsync(
            d => d.Status == DeliveryStatuses.Failed || d.Status == DeliveryStatuses.Returned,
            cancellationToken);
        var deliveredToday = await deliveries.CountAsync(
            d => d.Status == DeliveryStatuses.Delivered
                 && d.DeliveredAtUtc >= todayStart
                 && d.DeliveredAtUtc < todayEnd,
            cancellationToken);

        var attention = delayed + failed;
        var severity = failed > 0
            ? OrderFlowSeverities.Critical
            : delayed > 0
                ? OrderFlowSeverities.Critical
                : OrderFlowSeverities.None;

        var messages = new List<string>();
        if (ready > 0)
            messages.Add($"{ready} order{(ready == 1 ? "" : "s")} ready for delivery");
        if (delayed > 0)
            messages.Add($"{delayed} deliver{(delayed == 1 ? "y" : "ies")} past scheduled date");
        if (failed > 0)
            messages.Add($"{failed} failed or returned");
        if (inTransit > 0)
            messages.Add($"{inTransit} currently in transit");
        if (deliveredToday > 0)
            messages.Add($"{deliveredToday} delivered today");
        if (messages.Count == 0)
            messages.Add(total == 0 ? "No open deliveries" : "Deliveries on track");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Delivery,
            Title: "Delivery",
            SummaryText: delayed > 0
                ? $"{delayed} delayed"
                : ready > 0
                    ? $"{ready} ready to dispatch"
                    : total == 0 ? "No open deliveries" : "Deliveries in progress",
            Total: total,
            AttentionCount: attention,
            AttentionSeverity: severity,
            Stats:
            [
                new OrderFlowStatDto("ready", "Ready", ready, OrderFlowSeverities.Info),
                new OrderFlowStatDto("scheduled", "Scheduled", scheduled, OrderFlowSeverities.Pending),
                new OrderFlowStatDto("delivered_today", "Delivered today", deliveredToday, OrderFlowSeverities.Success),
                new OrderFlowStatDto("delayed", "Delayed", delayed, OrderFlowSeverities.Critical),
            ],
            Messages: messages);
    }

    private static async Task<OrderFlowStageDto> BuildCompletedStageAsync(
        IQueryable<SalesOrder> orders,
        DateTimeOffset todayStart,
        DateTimeOffset todayEnd,
        DateTimeOffset monthStart,
        CancellationToken cancellationToken)
    {
        var completed = orders.Where(o =>
            o.Status == SalesOrderStatuses.Delivered || o.Status == SalesOrderStatuses.Completed);

        var completedToday = await completed.CountAsync(
            o => o.ModifiedOnUtc >= todayStart && o.ModifiedOnUtc < todayEnd,
            cancellationToken);
        var completedThisMonth = await completed.CountAsync(
            o => o.ModifiedOnUtc >= monthStart,
            cancellationToken);
        var delivered = await completed.CountAsync(
            o => o.Status == SalesOrderStatuses.Delivered, cancellationToken);
        var fullyCompleted = await completed.CountAsync(
            o => o.Status == SalesOrderStatuses.Completed, cancellationToken);

        var messages = new List<string>();
        if (completedToday > 0)
            messages.Add($"{completedToday} order{(completedToday == 1 ? "" : "s")} completed today");
        if (completedThisMonth > 0)
            messages.Add($"{completedThisMonth} completed this month");
        if (messages.Count == 0)
            messages.Add("No completed orders yet this period");

        return new OrderFlowStageDto(
            StageKey: OrderFlowStageKeys.Completed,
            Title: "Completed",
            SummaryText: "End of flow",
            Total: completedThisMonth,
            AttentionCount: 0,
            AttentionSeverity: OrderFlowSeverities.None,
            Stats:
            [
                new OrderFlowStatDto("today", "Completed today", completedToday, OrderFlowSeverities.Success),
                new OrderFlowStatDto("month", "This month", completedThisMonth, OrderFlowSeverities.Success),
                new OrderFlowStatDto("delivered", "Delivered", delivered, OrderFlowSeverities.Info),
                new OrderFlowStatDto("completed", "Fully completed", fullyCompleted, OrderFlowSeverities.Success),
            ],
            Messages: messages);
    }

    private static IReadOnlyList<DateTimeOffset> BuildMonthWindow(DateTimeOffset now, int count = 6)
    {
        var end = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return Enumerable.Range(0, count)
            .Select(i => end.AddMonths(i - (count - 1)))
            .ToList();
    }

    private static IReadOnlyList<ChartDataPointDto> MonthValueSeries(
        IReadOnlyList<DateTimeOffset> months,
        IEnumerable<(DateTimeOffset Date, decimal Amount)> items)
    {
        var list = items.ToList();
        return months
            .Select(month =>
            {
                var value = list
                    .Where(item => item.Date.Year == month.Year && item.Date.Month == month.Month)
                    .Sum(item => item.Amount);
                return new ChartDataPointDto(month.ToString("MMM"), Math.Round(value, 2));
            })
            .ToList();
    }

    private static IReadOnlyList<ChartDataPointDto> MonthCountSeries(
        IReadOnlyList<DateTimeOffset> months,
        IEnumerable<DateTimeOffset> dates)
    {
        return MonthValueSeries(months, dates.Select(date => (date, 1m)));
    }

    private static IReadOnlyList<ChartDataPointDto> ToStatusChart(
        IEnumerable<(string Status, int Count)> rows)
    {
        return rows
            .Select(row => new ChartDataPointDto(HumanizeStatus(row.Status), row.Count))
            .ToList();
    }

    private static string HumanizeStatus(string status)
    {
        if (string.IsNullOrWhiteSpace(status)) return "Unknown";
        return string.Join(
            ' ',
            status.Split('_', StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Length == 1
                    ? part.ToUpperInvariant()
                    : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant()));
    }

    private static string TruncateLabel(string label, int max = 22)
    {
        if (string.IsNullOrWhiteSpace(label)) return "Unknown";
        return label.Length <= max ? label : label[..(max - 1)] + "…";
    }

    private static decimal GrowthPercent(decimal current, decimal previous)
    {
        if (previous == 0m) return current == 0m ? 0m : 100m;
        return Math.Round(((current - previous) / previous) * 100m, 2);
    }
}
