import type { PaginatedResponse } from "@/types/common";
import type {
  ManufacturingJob,
  ManufacturingTaskAction,
  ProductionCompletionInput,
} from "@/types/manufacturing";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  BulkCompleteTasksInput,
  ManufacturingJobFormData,
  ManufacturingListFilters,
  ManufacturingService,
} from "@/services/interfaces/manufacturingService";

function mapManufacturingJob(raw: Record<string, unknown>): ManufacturingJob {
  return {
    id: String(raw.id),
    jobNumber: String(raw.jobNumber ?? ""),
    salesOrderId: String(raw.salesOrderId),
    salesOrderNumber: String(raw.salesOrderNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    productId: String(raw.productId),
    productSku: String(raw.productSku ?? ""),
    productName: String(raw.productName ?? ""),
    productVersionId: String(raw.productVersionId ?? ""),
    productVersionLabel: String(raw.productVersionLabel ?? ""),
    quantity: Number(raw.quantity ?? 0),
    status: (raw.status as ManufacturingJob["status"]) ?? "draft",
    priority: (raw.priority as ManufacturingJob["priority"]) ?? "medium",
    tasks: (raw.tasks as ManufacturingJob["tasks"]) ?? [],
    reworks: (raw.reworks as ManufacturingJob["reworks"]) ?? [],
    materialRequirements: (raw.materialRequirements as ManufacturingJob["materialRequirements"]) ?? [],
    qualityInspection: raw.qualityInspection as ManufacturingJob["qualityInspection"],
    plannedStartDate: String(raw.plannedStartDate ?? new Date().toISOString()),
    plannedEndDate: String(raw.plannedEndDate ?? new Date().toISOString()),
    actualStartDate: raw.actualStartDate as string | undefined,
    actualEndDate: raw.actualEndDate as string | undefined,
    progressPercent: Number(raw.progressPercent ?? 0),
    estimatedCost: Number(raw.estimatedCost ?? 0),
    actualCost: Number(raw.actualCost ?? 0),
    assignedTo: raw.assignedTo == null ? undefined : String(raw.assignedTo),
    assignedToName: raw.assignedToName as string | undefined,
    notes: raw.notes as string | undefined,
    materialOutcome: raw.materialOutcome as ManufacturingJob["materialOutcome"],
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<ManufacturingJob> {
  return {
    items: (raw.items ?? []).map((item) => mapManufacturingJob(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpManufacturingService: ManufacturingService = {
  async list(filters: ManufacturingListFilters) {
    return mapPage(
      await apiRequest(
        `/api/manufacturing/jobs${buildQuery(filters as unknown as Record<string, unknown>)}`,
      ),
    );
  },
  async getById(id: string) {
    return mapManufacturingJob(await apiRequest(`/api/manufacturing/jobs/${id}`));
  },
  async create(data: ManufacturingJobFormData) {
    return mapManufacturingJob(
      await apiRequest("/api/manufacturing/jobs", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<ManufacturingJobFormData>) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/manufacturing/jobs/${id}`, { method: "DELETE" });
  },
  async reserveMaterials(id: string) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/reserve-materials`, { method: "POST" }),
    );
  },
  async startJob(id: string) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/start`, { method: "POST" }),
    );
  },
  async completeJob(id: string, completion?: ProductionCompletionInput) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/complete`, {
        method: "POST",
        body: JSON.stringify(completion ?? {}),
      }),
    );
  },
  async holdJob(id: string, reason?: string) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/hold`, {
        method: "POST",
        body: JSON.stringify({ reason }),
      }),
    );
  },
  async applyTaskAction(id: string, action: ManufacturingTaskAction) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/task-actions`, {
        method: "POST",
        body: JSON.stringify(action),
      }),
    );
  },
  async completeTasks(id: string, input: BulkCompleteTasksInput) {
    return mapManufacturingJob(
      await apiRequest(`/api/manufacturing/jobs/${id}/complete-tasks`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    );
  },
};
