namespace Reporting.Application.Dashboard;

public sealed record ChartDataPointDto(string Label, decimal Value, string? Color = null);

public sealed record DashboardTableRowDto(
    string Id,
    string Reference,
    string Title,
    string Status,
    decimal? Amount,
    string Date,
    string? Customer = null,
    string? Priority = null,
    string? Href = null);

public sealed record DashboardActivityChangeDto(string Field, string? From = null, string? To = null);

public sealed record DashboardActivityItemDto(
    string Id,
    string Description,
    string Timestamp,
    string Type,
    string? User = null,
    string? Href = null,
    string? Action = null,
    string? EntityLabel = null,
    string? Severity = null,
    string? EntityId = null,
    IReadOnlyList<DashboardActivityChangeDto>? Changes = null);

public sealed record DashboardNotificationPreviewDto(
    string Id,
    string Title,
    string Message,
    string Type,
    string CreatedAt,
    bool IsRead,
    string? ActionUrl = null);

public sealed record DashboardSummaryDto(
    string GeneratedAt,
    string PeriodLabel,
    int TotalCustomers,
    int ActiveQuotations,
    int PendingApprovals,
    int PendingEstimations,
    int ConfirmedSalesOrders,
    int OpenSalesOrders,
    int ManufacturingJobsInProgress,
    int DelayedJobs,
    int QualityCheckJobs,
    int ReadyToShip,
    int DeliveriesDueToday,
    int DeliveriesInTransit,
    int UpcomingDeliveryCount,
    int LowStockItems,
    int ReprocessingInProgress,
    int ProductCount,
    decimal MonthlySalesValue,
    decimal RevenueGrowthPercent,
    decimal OrdersGrowthPercent,
    decimal ProductionCapacityPercent,
    IReadOnlyList<ChartDataPointDto> MonthlyQuotationValue,
    IReadOnlyList<ChartDataPointDto> RevenueByMonth,
    IReadOnlyList<ChartDataPointDto> OrdersByMonth,
    IReadOnlyList<ChartDataPointDto> QuotationConversion,
    IReadOnlyList<ChartDataPointDto> OrdersByStatus,
    IReadOnlyList<ChartDataPointDto> ManufacturingByStatus,
    IReadOnlyList<ChartDataPointDto> DeliveriesByStatus,
    IReadOnlyList<ChartDataPointDto> InventoryByStatus,
    IReadOnlyList<ChartDataPointDto> TopProducts,
    IReadOnlyList<DashboardTableRowDto> RecentQuotations,
    IReadOnlyList<DashboardTableRowDto> RecentlyApprovedOrders,
    IReadOnlyList<DashboardTableRowDto> JobsRequiringAttention,
    IReadOnlyList<DashboardTableRowDto> UpcomingDeliveries,
    IReadOnlyList<DashboardTableRowDto> PendingCosting,
    IReadOnlyList<DashboardTableRowDto> LowStockRows,
    IReadOnlyList<DashboardActivityItemDto> RecentActivity,
    IReadOnlyList<DashboardNotificationPreviewDto> Notifications);
