import type { PaginatedResponse } from "@/types/common";
import type { UnitOfMeasure } from "@/lib/unitsOfMeasure";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  UnitOfMeasureFormData,
  UnitOfMeasureListFilters,
  UnitOfMeasureService,
} from "@/services/interfaces/unitOfMeasureService";

function mapUnit(raw: Record<string, unknown>): UnitOfMeasure {
  return {
    id: String(raw.id),
    code: String(raw.code ?? ""),
    name: String(raw.name ?? ""),
    status: (raw.status as UnitOfMeasure["status"]) ?? "active",
  };
}

function mapPage(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<UnitOfMeasure> {
  return {
    items: (raw.items ?? []).map((item) => mapUnit(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpUnitOfMeasureService: UnitOfMeasureService = {
  async list(filters: UnitOfMeasureListFilters) {
    return mapPage(
      await apiRequest(
        `/api/units-of-measure${buildQuery(filters as unknown as Record<string, unknown>)}`,
      ),
    );
  },
  async getById(id: string) {
    return mapUnit(await apiRequest(`/api/units-of-measure/${id}`));
  },
  async create(data: UnitOfMeasureFormData) {
    return mapUnit(
      await apiRequest("/api/units-of-measure", {
        method: "POST",
        body: JSON.stringify({
          code: data.code,
          name: data.name,
          status: data.status ?? "active",
        }),
      }),
    );
  },
  async update(id: string, data: Partial<UnitOfMeasureFormData>) {
    return mapUnit(
      await apiRequest(`/api/units-of-measure/${id}`, {
        method: "PUT",
        body: JSON.stringify(data),
      }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/units-of-measure/${id}`, { method: "DELETE" });
  },
};
