import type { PaginatedResponse } from "@/types/common";
import type { SalesOrder } from "@/types/sales-order";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  SalesOrderFormData,
  SalesOrderListFilters,
  SalesOrderService,
} from "@/services/interfaces/salesOrderService";

function mapSalesOrder(raw: Record<string, unknown>): SalesOrder {
  return {
    id: String(raw.id),
    orderNumber: String(raw.orderNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    customerEmail: String(raw.customerEmail ?? ""),
    quotationId: raw.quotationId == null ? undefined : String(raw.quotationId),
    quotationNumber: raw.quotationNumber as string | undefined,
    costingRequestId: raw.costingRequestId == null ? undefined : String(raw.costingRequestId),
    status: (raw.status as SalesOrder["status"]) ?? "draft",
    priority: (raw.priority as SalesOrder["priority"]) ?? "medium",
    lineItems: (raw.lineItems as SalesOrder["lineItems"]) ?? [],
    subtotal: Number(raw.subtotal ?? 0),
    discountAmount: Number(raw.discountAmount ?? 0),
    taxAmount: Number(raw.taxAmount ?? 0),
    totalAmount: Number(raw.totalAmount ?? 0),
    currency: String(raw.currency ?? "LKR"),
    paymentStatus: (raw.paymentStatus as SalesOrder["paymentStatus"]) ?? "unpaid",
    billingAddress: (raw.billingAddress as SalesOrder["billingAddress"]) ?? {
      line1: "",
      city: "",
      state: "",
      postalCode: "",
      country: "",
    },
    shippingAddress: raw.shippingAddress as SalesOrder["shippingAddress"],
    requestedDeliveryDate: raw.requestedDeliveryDate as string | undefined,
    notes: raw.notes as string | undefined,
    assignedTo: raw.assignedTo == null ? undefined : String(raw.assignedTo),
    assignedToName: raw.assignedToName as string | undefined,
    manufacturingJobIds: ((raw.manufacturingJobIds as string[]) ?? []).map(String),
    deliveryIds: ((raw.deliveryIds as string[]) ?? []).map(String),
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    confirmedAt: raw.confirmedAt as string | undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<SalesOrder> {
  return {
    items: (raw.items ?? []).map((item) => mapSalesOrder(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpSalesOrderService: SalesOrderService = {
  async list(filters: SalesOrderListFilters) {
    return mapPage(
      await apiRequest(`/api/sales-orders${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapSalesOrder(await apiRequest(`/api/sales-orders/${id}`));
  },
  async create(data: SalesOrderFormData) {
    return mapSalesOrder(
      await apiRequest("/api/sales-orders", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<SalesOrderFormData>) {
    return mapSalesOrder(
      await apiRequest(`/api/sales-orders/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/sales-orders/${id}`, { method: "DELETE" });
  },
  async confirm(id: string) {
    return mapSalesOrder(await apiRequest(`/api/sales-orders/${id}/confirm`, { method: "POST" }));
  },
  async cancel(id: string, reason?: string) {
    return mapSalesOrder(
      await apiRequest(`/api/sales-orders/${id}/cancel`, {
        method: "POST",
        body: JSON.stringify({ reason }),
      }),
    );
  },
  async assign(id: string, userId: string) {
    return mapSalesOrder(
      await apiRequest(`/api/sales-orders/${id}/assign`, {
        method: "POST",
        body: JSON.stringify({ userId }),
      }),
    );
  },
};
