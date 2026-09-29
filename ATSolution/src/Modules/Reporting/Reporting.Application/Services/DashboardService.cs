using ATSolution.Application.Abstractions.Persistence;
using Catalog.Domain.Products;
using Customers.Domain.Customers;
using Delivery.Domain.Common;
using Inventory.Domain.Common;
using Inventory.Domain.Items;
using Manufacturing.Domain.Common;
using Manufacturing.Domain.Jobs;
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
    private readonly IRepository<Customer, Guid> _customers;
    private readonly IRepository<Quotation, Guid> _quotations;
    private readonly IRepository<SalesOrder, Guid> _salesOrders;
    private readonly IRepository<CostingRequest, Guid> _costing;
    private readonly IRepository<InventoryItem, Guid> _inventory;
    private readonly IRepository<ManufacturingJob, Guid> _jobs;
    private readonly IRepository<DeliveryEntity, Guid> _deliveries;
    private readonly IRepository<Product, Guid> _products;

    public DashboardService(
        IRepository<Customer, Guid> customers,
        IRepository<Quotation, Guid> quotations,
        IRepository<SalesOrder, Guid> salesOrders,
        IRepository<CostingRequest, Guid> costing,
        IRepository<InventoryItem, Guid> inventory,
        IRepository<ManufacturingJob, Guid> jobs,
        IRepository<DeliveryEntity, Guid> deliveries,
        IRepository<Product, Guid> products)
    {
        _customers = customers;
        _quotations = quotations;
        _salesOrders = salesOrders;
        _costing = costing;
        _inventory = inventory;
        _jobs = jobs;
        _deliveries = deliveries;
        _products = products;
    }

    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);
        var monthStart = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);

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
        var monthlySalesValue = await orders
            .Where(o => o.ConfirmedAtUtc >= monthStart || (o.ConfirmedAtUtc == null && o.CreatedOnUtc >= monthStart))
            .Where(o => o.Status != SalesOrderStatuses.Cancelled && o.Status != SalesOrderStatuses.Draft)
            .SumAsync(o => (decimal?)o.TotalAmount, cancellationToken) ?? 0m;

        var emptyCharts = Array.Empty<ChartDataPointDto>();
        var emptyTables = Array.Empty<DashboardTableRowDto>();
        var emptyActivity = Array.Empty<DashboardActivityItemDto>();
        var emptyNotifications = Array.Empty<DashboardNotificationPreviewDto>();

        return new DashboardSummaryDto(
            GeneratedAt: now.ToString("O"),
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
            RevenueGrowthPercent: 0,
            OrdersGrowthPercent: 0,
            ProductionCapacityPercent: 0,
            MonthlyQuotationValue: emptyCharts,
            RevenueByMonth: emptyCharts,
            OrdersByMonth: emptyCharts,
            QuotationConversion: emptyCharts,
            OrdersByStatus: emptyCharts,
            ManufacturingByStatus: emptyCharts,
            DeliveriesByStatus: emptyCharts,
            InventoryByStatus: emptyCharts,
            TopProducts: emptyCharts,
            RecentQuotations: emptyTables,
            RecentlyApprovedOrders: emptyTables,
            JobsRequiringAttention: emptyTables,
            UpcomingDeliveries: emptyTables,
            PendingCosting: emptyTables,
            LowStockRows: emptyTables,
            RecentActivity: emptyActivity,
            Notifications: emptyNotifications);
    }
}
