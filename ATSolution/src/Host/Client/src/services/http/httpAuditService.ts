import type { PaginatedResponse } from "@/types/common";
import type {
  AuditAction,
  AuditEntity,
  AuditLogEntry,
  AuditLogSummary,
  AuditSeverity,
} from "@/types/audit";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type { AuditLogListFilters, AuditService } from "@/services/interfaces/auditService";

function mapEntry(raw: Record<string, unknown>): AuditLogEntry {
  return {
    id: String(raw.id),
    timestamp: String(raw.timestamp),
    userId: String(raw.userId),
    userName: String(raw.userName),
    action: raw.action as AuditAction,
    entity: raw.entity as AuditEntity,
    entityId: String(raw.entityId),
    entityLabel: raw.entityLabel as string | undefined,
    details: String(raw.details ?? ""),
    severity: (raw.severity as AuditSeverity) ?? "info",
    ipAddress: raw.ipAddress as string | undefined,
    userAgent: raw.userAgent as string | undefined,
    changes: (raw.changes as AuditLogEntry["changes"]) ?? undefined,
  };
}

function mapPage(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<AuditLogEntry> {
  return {
    items: (raw.items ?? []).map((item) => mapEntry(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpAuditService: AuditService = {
  async list(filters: AuditLogListFilters) {
    return mapPage(
      await apiRequest(`/api/audit-logs${buildQuery(filters as Record<string, unknown>)}`),
    );
  },

  async getById(id: string) {
    return mapEntry(await apiRequest(`/api/audit-logs/${id}`));
  },

  async summary(filters = {}) {
    const raw = await apiRequest(
      `/api/audit-logs/summary${buildQuery(filters as Record<string, unknown>)}`,
    );
    return {
      total: Number(raw.total ?? 0),
      info: Number(raw.info ?? 0),
      warning: Number(raw.warning ?? 0),
      critical: Number(raw.critical ?? 0),
      today: Number(raw.today ?? 0),
    } satisfies AuditLogSummary;
  },
};
