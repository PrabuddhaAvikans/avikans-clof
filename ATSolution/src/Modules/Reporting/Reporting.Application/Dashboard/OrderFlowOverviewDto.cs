namespace Reporting.Application.Dashboard;

public sealed record OrderFlowOverviewDto(
    DateTimeOffset GeneratedAt,
    OrderFlowStageDto Quotation,
    OrderFlowStageDto SalesOrder,
    OrderFlowStageDto Estimation,
    OrderFlowStageDto Costing,
    OrderFlowStageDto Production,
    OrderFlowStageDto Delivery,
    OrderFlowStageDto Completed);
