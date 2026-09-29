import type {
  ChartDataPoint,
  DashboardActivityChange,
  DashboardActivityItem,
  DashboardNotificationPreview,
  DashboardSummary,
  DashboardTableRow,
} from "@/types/dashboard";
import { apiRequest } from "@/services/apiClient";
import type { DashboardService } from "@/services/interfaces/dashboardService";

function mapChartPoint(raw: Record<string, unknown>): ChartDataPoint {
  return {
    label: String(raw.label ?? ""),
    value: Number(raw.value ?? 0),
    color: raw.color as string | undefined,
  };
}

function mapTableRow(raw: Record<string, unknown>): DashboardTableRow {
  return {
    id: String(raw.id),
    reference: String(raw.reference ?? ""),
    title: String(raw.title ?? ""),
    status: String(raw.status ?? ""),
    amount: raw.amount == null ? undefined : Number(raw.amount),
    date: String(raw.date ?? ""),
    customer: raw.customer as string | undefined,
    priority: raw.priority as string | undefined,
    href: raw.href as string | undefined,
  };
}

function mapActivityChange(raw: Record<string, unknown>): DashboardActivityChange {
  return {
    field: String(raw.field ?? ""),
    from: raw.from as string | undefined,
    to: raw.to as string | undefined,
  };
}

function mapActivityItem(raw: Record<string, unknown>): DashboardActivityItem {
  return {
    id: String(raw.id),
    description: String(raw.description ?? ""),
    timestamp: String(raw.timestamp ?? new Date().toISOString()),
    type: String(raw.type ?? ""),
    user: raw.user as string | undefined,
    href: raw.href as string | undefined,
    action: raw.action as string | undefined,
    entityLabel: raw.entityLabel as string | undefined,
    severity: raw.severity as string | undefined,
    entityId: raw.entityId == null ? undefined : String(raw.entityId),
    changes: ((raw.changes as unknown[]) ?? []).map((change) =>
      mapActivityChange(change as Record<string, unknown>),
    ),
  };
}

function mapNotification(raw: Record<string, unknown>): DashboardNotificationPreview {
  return {
    id: String(raw.id),
    title: String(raw.title ?? ""),
    message: String(raw.message ?? ""),
    type: String(raw.type ?? ""),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    isRead: Boolean(raw.isRead),
    actionUrl: raw.actionUrl as string | undefined,
  };
}

function mapChartArray(raw: unknown): ChartDataPoint[] {
  return ((raw as unknown[]) ?? []).map((item) => mapChartPoint(item as Record<string, unknown>));
}

function mapTableArray(raw: unknown): DashboardTableRow[] {
  return ((raw as unknown[]) ?? []).map((item) => mapTableRow(item as Record<string, unknown>));
}

function mapSummary(raw: Record<string, unknown>): DashboardSummary {
  return {
    generatedAt: String(raw.generatedAt ?? new Date().toISOString()),
    periodLabel: String(raw.periodLabel ?? ""),

    totalCustomers: Number(raw.totalCustomers ?? 0),
    activeQuotations: Number(raw.activeQuotations ?? 0),
    pendingApprovals: Number(raw.pendingApprovals ?? 0),
    pendingEstimations: Number(raw.pendingEstimations ?? 0),
    confirmedSalesOrders: Number(raw.confirmedSalesOrders ?? 0),
    openSalesOrders: Number(raw.openSalesOrders ?? 0),
    manufacturingJobsInProgress: Number(raw.manufacturingJobsInProgress ?? 0),
    delayedJobs: Number(raw.delayedJobs ?? 0),
    qualityCheckJobs: Number(raw.qualityCheckJobs ?? 0),
    readyToShip: Number(raw.readyToShip ?? 0),
    deliveriesDueToday: Number(raw.deliveriesDueToday ?? 0),
    deliveriesInTransit: Number(raw.deliveriesInTransit ?? 0),
    upcomingDeliveryCount: Number(raw.upcomingDeliveryCount ?? 0),
    lowStockItems: Number(raw.lowStockItems ?? 0),
    reprocessingInProgress: Number(raw.reprocessingInProgress ?? 0),
    productCount: Number(raw.productCount ?? 0),
    monthlySalesValue: Number(raw.monthlySalesValue ?? 0),
    revenueGrowthPercent: Number(raw.revenueGrowthPercent ?? 0),
    ordersGrowthPercent: Number(raw.ordersGrowthPercent ?? 0),
    productionCapacityPercent: Number(raw.productionCapacityPercent ?? 0),

    monthlyQuotationValue: mapChartArray(raw.monthlyQuotationValue),
    revenueByMonth: mapChartArray(raw.revenueByMonth),
    ordersByMonth: mapChartArray(raw.ordersByMonth),
    quotationConversion: mapChartArray(raw.quotationConversion),
    ordersByStatus: mapChartArray(raw.ordersByStatus),
    manufacturingByStatus: mapChartArray(raw.manufacturingByStatus),
    deliveriesByStatus: mapChartArray(raw.deliveriesByStatus),
    inventoryByStatus: mapChartArray(raw.inventoryByStatus),
    topProducts: mapChartArray(raw.topProducts),

    recentQuotations: mapTableArray(raw.recentQuotations),
    recentlyApprovedOrders: mapTableArray(raw.recentlyApprovedOrders),
    jobsRequiringAttention: mapTableArray(raw.jobsRequiringAttention),
    upcomingDeliveries: mapTableArray(raw.upcomingDeliveries),
    pendingCosting: mapTableArray(raw.pendingCosting),
    lowStockRows: mapTableArray(raw.lowStockRows),
    recentActivity: ((raw.recentActivity as unknown[]) ?? []).map((item) =>
      mapActivityItem(item as Record<string, unknown>),
    ),
    notifications: ((raw.notifications as unknown[]) ?? []).map((item) =>
      mapNotification(item as Record<string, unknown>),
    ),
  };
}

export const httpDashboardService: DashboardService = {
  async getSummary() {
    return mapSummary(await apiRequest("/api/dashboard/summary"));
  },
};
