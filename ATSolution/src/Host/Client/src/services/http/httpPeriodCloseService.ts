import type { PaginatedResponse } from "@/types/common";
import type {
  BusinessPeriod,
  CloseDayResult,
  CloseMonthResult,
  DailyClosingSummary,
  DayCloseWorkspace,
  InventoryDailySnapshot,
  InventoryMonthlySnapshot,
  MonthlyCloseWorkspace,
  MonthlyClosingSummary,
  MonthlyPeriod,
  PeriodAdjustment,
  PeriodAuditLog,
  PeriodCloseSettings,
  ProductionDailySnapshot,
  ProductionMonthlySnapshot,
  WorkerSessionCheckpoint,
} from "@/types/period-close";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  BusinessPeriodListFilters,
  CloseDayOptions,
  MonthlyPeriodListFilters,
  PeriodCloseService,
} from "@/services/interfaces/periodCloseService";

const BASE = "/api/period-close";

const ID_KEY = /^(id|.+Id)$/i;
const DATE_KEY =
  /^(businessDate|postingBusinessDate|originalBusinessDate|.+At)$/i;

function isIdOrDateKey(key: string): boolean {
  return ID_KEY.test(key) || DATE_KEY.test(key);
}

/** Recursively stringify Guid/id and DateTimeOffset/date fields on known shapes. */
function mapUnknown<T>(value: unknown): T {
  if (value === null || value === undefined) return value as T;
  if (Array.isArray(value)) {
    return value.map((item) => mapUnknown(item)) as T;
  }
  if (typeof value === "object") {
    const result: Record<string, unknown> = {};
    for (const [key, child] of Object.entries(value as Record<string, unknown>)) {
      if (child === null || child === undefined) {
        result[key] = child;
      } else if (typeof child === "object") {
        result[key] = mapUnknown(child);
      } else if (isIdOrDateKey(key)) {
        result[key] = String(child);
      } else {
        result[key] = child;
      }
    }
    return result as T;
  }
  return value as T;
}

function mapPage<T>(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<T> {
  return {
    items: (raw.items ?? []).map((item) => mapUnknown<T>(item)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpPeriodCloseService: PeriodCloseService = {
  async getSettings(branchId) {
    return mapUnknown<PeriodCloseSettings>(
      await apiRequest(`${BASE}/settings${buildQuery({ branchId })}`),
    );
  },

  async updateSettings(settings) {
    return mapUnknown<PeriodCloseSettings>(
      await apiRequest(`${BASE}/settings`, {
        method: "PUT",
        body: JSON.stringify(settings),
      }),
    );
  },

  async listDayPeriods(filters: BusinessPeriodListFilters) {
    return mapPage<BusinessPeriod>(
      await apiRequest(`${BASE}/days${buildQuery(filters as Record<string, unknown>)}`),
    );
  },

  async listMonthlyPeriods(filters: MonthlyPeriodListFilters) {
    return mapPage<MonthlyPeriod>(
      await apiRequest(`${BASE}/months${buildQuery(filters as Record<string, unknown>)}`),
    );
  },

  async getCurrentDay(branchId) {
    return mapUnknown<DayCloseWorkspace>(
      await apiRequest(`${BASE}/days/current${buildQuery({ branchId })}`),
    );
  },

  async getDayWorkspace(periodId) {
    return mapUnknown<DayCloseWorkspace>(
      await apiRequest(`${BASE}/days/${periodId}`),
    );
  },

  async getCurrentMonth(branchId) {
    return mapUnknown<MonthlyCloseWorkspace>(
      await apiRequest(`${BASE}/months/current${buildQuery({ branchId })}`),
    );
  },

  async getMonthWorkspace(periodId) {
    return mapUnknown<MonthlyCloseWorkspace>(
      await apiRequest(`${BASE}/months/${periodId}`),
    );
  },

  async runDayValidation(periodId) {
    return mapUnknown<DayCloseWorkspace>(
      await apiRequest(`${BASE}/days/${periodId}/validate`, { method: "POST" }),
    );
  },

  async closeDay(periodId, options?: CloseDayOptions) {
    return mapUnknown<CloseDayResult>(
      await apiRequest(`${BASE}/days/${periodId}/close`, {
        method: "POST",
        body: JSON.stringify(options ?? {}),
      }),
    );
  },

  async reopenDay(periodId, input) {
    return mapUnknown<BusinessPeriod>(
      await apiRequest(`${BASE}/days/${periodId}/reopen`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    );
  },

  async runMonthValidation(periodId) {
    return mapUnknown<MonthlyCloseWorkspace>(
      await apiRequest(`${BASE}/months/${periodId}/validate`, { method: "POST" }),
    );
  },

  async closeMonth(periodId) {
    return mapUnknown<CloseMonthResult>(
      await apiRequest(`${BASE}/months/${periodId}/close`, { method: "POST" }),
    );
  },

  async reopenMonth(periodId, input) {
    return mapUnknown<MonthlyPeriod>(
      await apiRequest(`${BASE}/months/${periodId}/reopen`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    );
  },

  async listDayAudit(periodId) {
    return mapUnknown<PeriodAuditLog[]>(
      await apiRequest(`${BASE}/days/${periodId}/audit`),
    );
  },

  async listMonthAudit(periodId) {
    return mapUnknown<PeriodAuditLog[]>(
      await apiRequest(`${BASE}/months/${periodId}/audit`),
    );
  },

  async getDailySummary(periodId) {
    const raw = await apiRequest(`${BASE}/days/${periodId}/summary`);
    if (raw == null) return null;
    return mapUnknown<DailyClosingSummary>(raw);
  },

  async getMonthlySummary(periodId) {
    const raw = await apiRequest(`${BASE}/months/${periodId}/summary`);
    if (raw == null) return null;
    return mapUnknown<MonthlyClosingSummary>(raw);
  },

  async listProductionDailySnapshots(periodId) {
    return mapUnknown<ProductionDailySnapshot[]>(
      await apiRequest(`${BASE}/days/${periodId}/production-snapshots`),
    );
  },

  async listProductionMonthlySnapshots(periodId) {
    return mapUnknown<ProductionMonthlySnapshot[]>(
      await apiRequest(`${BASE}/months/${periodId}/production-snapshots`),
    );
  },

  async listInventoryDailySnapshots(periodId) {
    return mapUnknown<InventoryDailySnapshot[]>(
      await apiRequest(`${BASE}/days/${periodId}/inventory-snapshots`),
    );
  },

  async listInventoryMonthlySnapshots(periodId) {
    return mapUnknown<InventoryMonthlySnapshot[]>(
      await apiRequest(`${BASE}/months/${periodId}/inventory-snapshots`),
    );
  },

  async listSessionCheckpoints(periodId) {
    return mapUnknown<WorkerSessionCheckpoint[]>(
      await apiRequest(`${BASE}/days/${periodId}/session-checkpoints`),
    );
  },

  async createAdjustment(input) {
    return mapUnknown<PeriodAdjustment>(
      await apiRequest(`${BASE}/adjustments`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    );
  },

  async listAdjustments(branchId) {
    return mapUnknown<PeriodAdjustment[]>(
      await apiRequest(`${BASE}/adjustments${buildQuery({ branchId })}`),
    );
  },

  async assertWritable(branchId, businessDate) {
    await apiRequest(`${BASE}/assert-writable`, {
      method: "POST",
      body: JSON.stringify({ branchId, businessDate }),
    });
  },
};
