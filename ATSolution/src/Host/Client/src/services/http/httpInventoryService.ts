import type { PaginatedResponse } from "@/types/common";
import type {
  InventoryItem,
  InventoryPriceHistoryEntry,
  StockMovement,
} from "@/types/inventory";
import { normalizeInventoryItem } from "@/types/inventory";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  InventoryFormData,
  InventoryListFilters,
  InventoryService,
  StockMovementFilters,
  StockMovementReference,
} from "@/services/interfaces/inventoryService";
import type { StockMovementTypeValue } from "@/types/inventory";

function mapItem(raw: Record<string, unknown>): InventoryItem {
  return normalizeInventoryItem({
    id: String(raw.id),
    sku: String(raw.sku),
    name: String(raw.name),
    description: raw.description as string | undefined,
    category: String(raw.category ?? ""),
    itemType: (raw.itemType as InventoryItem["itemType"]) ?? "raw_material",
    unit: String(raw.unit ?? "pcs"),
    brand: raw.brand as string | undefined,
    supplier: raw.supplier as string | undefined,
    taxCode: raw.taxCode as string | undefined,
    quantityOnHand: Number(raw.quantityOnHand ?? 0),
    quantityReserved: Number(raw.quantityReserved ?? 0),
    quantityAvailable: Number(raw.quantityAvailable ?? 0),
    warehouse: String(raw.warehouse ?? ""),
    location: String(raw.location ?? ""),
    minStock: Number(raw.minStock ?? 0),
    maxStock: Number(raw.maxStock ?? 0),
    reorderLevel: Number(raw.reorderLevel ?? 0),
    reorderQuantity: Number(raw.reorderQuantity ?? 0),
    buyingPrice: raw.buyingPrice as number | undefined,
    costPrice: Number(raw.costPrice ?? raw.unitCost ?? 0),
    unitCost: Number(raw.unitCost ?? raw.costPrice ?? 0),
    pricingMethod: (raw.pricingMethod as InventoryItem["pricingMethod"]) ?? "manual",
    markupPercent: Number(raw.markupPercent ?? 0),
    markupFixedAmount: Number(raw.markupFixedAmount ?? 0),
    sellingPrice: Number(raw.sellingPrice ?? 0),
    pricingEffectiveDate: String(raw.pricingEffectiveDate ?? new Date().toISOString()),
    stockStatus: (raw.stockStatus as InventoryItem["stockStatus"]) ?? "out_of_stock",
    status: (raw.status as InventoryItem["status"]) ?? "active",
    lastRestockedAt: raw.lastRestockedAt as string | undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  });
}

function mapMovement(raw: Record<string, unknown>): StockMovement {
  return {
    id: String(raw.id),
    inventoryItemId: String(raw.inventoryItemId),
    inventoryItemName: String(raw.inventoryItemName ?? ""),
    inventoryItemSku: String(raw.inventoryItemSku ?? ""),
    type: raw.type as StockMovement["type"],
    quantity: Number(raw.quantity ?? 0),
    unit: String(raw.unit ?? ""),
    referenceType: raw.referenceType as string | undefined,
    referenceId: raw.referenceId as string | undefined,
    notes: raw.notes as string | undefined,
    performedBy: String(raw.performedBy ?? ""),
    performedByName: String(raw.performedByName ?? ""),
    performedAt: String(raw.performedAt ?? new Date().toISOString()),
    trace: raw.trace as StockMovement["trace"],
  };
}

function mapPriceHistory(raw: Record<string, unknown>): InventoryPriceHistoryEntry {
  return {
    id: String(raw.id),
    inventoryItemId: String(raw.inventoryItemId),
    buyingPrice: raw.buyingPrice as number | undefined,
    costPrice: Number(raw.costPrice ?? 0),
    sellingPrice: Number(raw.sellingPrice ?? 0),
    pricingMethod: (raw.pricingMethod as InventoryPriceHistoryEntry["pricingMethod"]) ?? "manual",
    markupPercent: Number(raw.markupPercent ?? 0),
    markupFixedAmount: Number(raw.markupFixedAmount ?? 0),
    effectiveDate: String(raw.effectiveDate ?? new Date().toISOString()),
    changedBy: String(raw.changedBy ?? ""),
    changedByName: String(raw.changedByName ?? ""),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
  };
}

function mapPage<T>(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
  mapItemFn: (item: Record<string, unknown>) => T,
): PaginatedResponse<T> {
  return {
    items: (raw.items ?? []).map((item) => mapItemFn(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpInventoryService: InventoryService = {
  async list(filters: InventoryListFilters) {
    return mapPage(
      await apiRequest(`/api/inventory${buildQuery(filters as unknown as Record<string, unknown>)}`),
      mapItem,
    );
  },
  async getById(id: string) {
    return mapItem(await apiRequest(`/api/inventory/${id}`));
  },
  async create(data: InventoryFormData) {
    return mapItem(
      await apiRequest("/api/inventory", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<InventoryFormData>) {
    return mapItem(
      await apiRequest(`/api/inventory/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/inventory/${id}`, { method: "DELETE" });
  },
  async getLowStock() {
    const raw = await apiRequest<unknown[]>("/api/inventory/low-stock");
    return raw.map((item) => mapItem(item as Record<string, unknown>));
  },
  async listMovements(filters: StockMovementFilters) {
    return mapPage(
      await apiRequest(
        `/api/inventory/movements${buildQuery(filters as unknown as Record<string, unknown>)}`,
      ),
      mapMovement,
    );
  },
  async recordMovement(
    inventoryItemId: string,
    type: StockMovementTypeValue,
    quantity: number,
    reference?: StockMovementReference,
  ) {
    return mapMovement(
      await apiRequest(`/api/inventory/${inventoryItemId}/movements`, {
        method: "POST",
        body: JSON.stringify({
          type,
          quantity,
          referenceType: reference?.referenceType,
          referenceId: reference?.referenceId,
          notes: reference?.notes,
          trace: reference?.trace,
        }),
      }),
    );
  },
  async getPriceHistory(inventoryItemId: string) {
    const raw = await apiRequest<unknown[]>(`/api/inventory/${inventoryItemId}/price-history`);
    return raw.map((item) => mapPriceHistory(item as Record<string, unknown>));
  },
  async findBySku(sku: string) {
    try {
      return mapItem(await apiRequest(`/api/inventory/by-sku/${encodeURIComponent(sku)}`));
    } catch {
      return null;
    }
  },
};
