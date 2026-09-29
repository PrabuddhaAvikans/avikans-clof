import type { PaginatedResponse } from "@/types/common";
import type { CostingRequest } from "@/types/costing";
import type { SalesOrder } from "@/types/sales-order";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  CoatingSubmitData,
  CostingListFilters,
  CostingService,
} from "@/services/interfaces/costingService";

function mapCosting(raw: Record<string, unknown>): CostingRequest {
  return {
    id: String(raw.id),
    requestNumber: String(raw.requestNumber ?? ""),
    customerName: String(raw.customerName ?? ""),
    projectName: String(raw.projectName ?? ""),
    requestType: String(raw.requestType ?? ""),
    requestedDate: String(raw.requestedDate ?? new Date().toISOString()),
    totalEstimate: Number(raw.totalEstimate ?? 0),
    proposedPrice: Number(raw.proposedPrice ?? 0),
    marginPercent: Number(raw.marginPercent ?? 0),
    targetMargin: Number(raw.targetMargin ?? 0),
    riskFlag: (raw.riskFlag as CostingRequest["riskFlag"]) ?? "medium",
    slaRemaining: String(raw.slaRemaining ?? ""),
    status: (raw.status as CostingRequest["status"]) ?? "pending",
    coatingStatus: (raw.coatingStatus as CostingRequest["coatingStatus"]) ?? "pending",
    currency: String(raw.currency ?? "LKR"),
    paymentTerms: String(raw.paymentTerms ?? ""),
    lineItems: (raw.lineItems as CostingRequest["lineItems"]) ?? [],
    coatingItems: (raw.coatingItems as CostingRequest["coatingItems"]) ?? [],
    estimationMaterials: (raw.estimationMaterials as CostingRequest["estimationMaterials"]) ?? [],
    estimationProductLines: (raw.estimationProductLines as CostingRequest["estimationProductLines"]) ?? [],
    attachments: (raw.attachments as CostingRequest["attachments"]) ?? [],
    notes: String(raw.notes ?? ""),
    requester: (raw.requester as CostingRequest["requester"]) ?? {
      name: "",
      title: "",
      email: "",
      avatarInitials: "",
    },
    approvalLevels: (raw.approvalLevels as CostingRequest["approvalLevels"]) ?? [],
    history: (raw.history as CostingRequest["history"]) ?? [],
    salesOrderId: raw.salesOrderId == null ? undefined : String(raw.salesOrderId),
    salesOrderNumber: raw.salesOrderNumber as string | undefined,
    quotationId: raw.quotationId == null ? undefined : String(raw.quotationId),
    quotationNumber: raw.quotationNumber as string | undefined,
    workflowDefinitionId: raw.workflowDefinitionId as string | undefined,
    workflowVersionId: raw.workflowVersionId as string | undefined,
    workflowInstanceId: raw.workflowInstanceId as string | undefined,
    workflowVersionNumber: raw.workflowVersionNumber as number | undefined,
    workflowName: raw.workflowName as string | undefined,
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<CostingRequest> {
  return {
    items: (raw.items ?? []).map((item) => mapCosting(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpCostingService: CostingService = {
  async list(filters: CostingListFilters) {
    return mapPage(
      await apiRequest(`/api/costing${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapCosting(await apiRequest(`/api/costing/${id}`));
  },
  async getBySalesOrderId(salesOrderId: string) {
    const raw = await apiRequest(`/api/costing/by-sales-order/${salesOrderId}`);
    return raw ? mapCosting(raw as Record<string, unknown>) : null;
  },
  async createFromSalesOrder(order: SalesOrder) {
    return mapCosting(
      await apiRequest(`/api/costing/from-sales-order/${order.id}`, { method: "POST" }),
    );
  },
  async syncFromSalesOrder(order: SalesOrder) {
    return mapCosting(
      await apiRequest(`/api/costing/sync-from-sales-order/${order.id}`, { method: "POST" }),
    );
  },
  async submitCoating(id: string, data: CoatingSubmitData) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/submit-coating`, {
        method: "POST",
        body: JSON.stringify(data),
      }),
    );
  },
  async approve(id: string, comment?: string) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/approve`, {
        method: "POST",
        body: JSON.stringify({ comment }),
      }),
    );
  },
  async reject(id: string, comment: string) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/reject`, {
        method: "POST",
        body: JSON.stringify({ comment }),
      }),
    );
  },
  async requestChanges(id: string, comment: string) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/request-changes`, {
        method: "POST",
        body: JSON.stringify({ comment }),
      }),
    );
  },
  async updateNotes(id: string, notes: string) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/notes`, {
        method: "PUT",
        body: JSON.stringify({ notes }),
      }),
    );
  },
  async addComment(id: string, comment: string) {
    return mapCosting(
      await apiRequest(`/api/costing/${id}/comments`, {
        method: "POST",
        body: JSON.stringify({ comment }),
      }),
    );
  },
};
