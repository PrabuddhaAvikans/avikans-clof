import type { PaginatedResponse } from "@/types/common";
import type { Category } from "@/types/category";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  CategoryFormData,
  CategoryListFilters,
  CategoryService,
} from "@/services/interfaces/categoryService";

function mapCategory(raw: Record<string, unknown>): Category {
  return {
    id: String(raw.id),
    name: String(raw.name),
    slug: String(raw.slug),
    description: raw.description as string | undefined,
    parentId: raw.parentId == null ? null : String(raw.parentId),
    parentName: raw.parentName as string | undefined,
    sortOrder: Number(raw.sortOrder ?? 0),
    status: (raw.status as Category["status"]) ?? "active",
    productCount: Number(raw.productCount ?? 0),
    imageUrl: raw.imageUrl as string | undefined,
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? raw.modifiedOnUtc ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<Category> {
  return {
    items: (raw.items ?? []).map((item) => mapCategory(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpCategoryService: CategoryService = {
  async list(filters: CategoryListFilters) {
    return mapPage(
      await apiRequest(`/api/categories${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapCategory(await apiRequest(`/api/categories/${id}`));
  },
  async getTree() {
    const raw = await apiRequest<unknown[]>("/api/categories/tree");
    return raw.map((item) => mapCategory(item as Record<string, unknown>));
  },
  async create(data: CategoryFormData) {
    return mapCategory(
      await apiRequest("/api/categories", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<CategoryFormData>) {
    return mapCategory(
      await apiRequest(`/api/categories/${id}`, {
        method: "PUT",
        body: JSON.stringify({
          ...data,
          clearParent: data.parentId === null,
        }),
      }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/categories/${id}`, { method: "DELETE" });
  },
};
