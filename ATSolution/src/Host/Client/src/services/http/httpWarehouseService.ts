import type { PaginatedResponse } from "@/types/common";
import type { Warehouse, WarehouseStatus } from "@/lib/warehouses";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  WarehouseFormData,
  WarehouseListFilters,
  WarehouseService,
} from "@/services/interfaces/warehouseService";

function mapWarehouse(raw: Record<string, unknown>): Warehouse {
  return {
    id: String(raw.id),
    code: String(raw.code ?? ""),
    name: String(raw.name ?? ""),
    address: String(raw.address ?? ""),
    status: (raw.status as WarehouseStatus) ?? "active",
  };
}

function mapPage(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<Warehouse> {
  return {
    items: (raw.items ?? []).map((item) => mapWarehouse(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpWarehouseService: WarehouseService = {
  async list(filters: WarehouseListFilters) {
    return mapPage(
      await apiRequest(`/api/warehouses${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapWarehouse(await apiRequest(`/api/warehouses/${id}`));
  },
  async create(data: WarehouseFormData) {
    return mapWarehouse(
      await apiRequest("/api/warehouses", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<WarehouseFormData>) {
    return mapWarehouse(
      await apiRequest(`/api/warehouses/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/warehouses/${id}`, { method: "DELETE" });
  },
};
