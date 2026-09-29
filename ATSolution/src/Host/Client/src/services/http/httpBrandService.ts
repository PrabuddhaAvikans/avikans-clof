import type { PaginatedResponse } from "@/types/common";
import type { Brand } from "@/types/brand";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  BrandFormData,
  BrandListFilters,
  BrandService,
} from "@/services/interfaces/brandService";

function mapBrand(raw: Record<string, unknown>): Brand {
  return {
    id: String(raw.id),
    name: String(raw.name),
    slug: String(raw.slug),
    description: raw.description as string | undefined,
    logoUrl: raw.logoUrl as string | undefined,
    website: raw.website as string | undefined,
    countryOfOrigin: raw.countryOfOrigin as string | undefined,
    status: (raw.status as Brand["status"]) ?? "active",
    productCount: Number(raw.productCount ?? 0),
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.modifiedOnUtc ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<Brand> {
  return {
    items: (raw.items ?? []).map((item) => mapBrand(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpBrandService: BrandService = {
  async list(filters: BrandListFilters) {
    return mapPage(
      await apiRequest(`/api/brands${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapBrand(await apiRequest(`/api/brands/${id}`));
  },
  async create(data: BrandFormData) {
    return mapBrand(
      await apiRequest("/api/brands", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<BrandFormData>) {
    return mapBrand(
      await apiRequest(`/api/brands/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/brands/${id}`, { method: "DELETE" });
  },
};
