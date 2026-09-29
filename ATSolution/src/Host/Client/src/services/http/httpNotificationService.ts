import type { PaginatedResponse } from "@/types/common";
import type {
  AppNotification,
  NotificationCategoryValue,
  NotificationTypeValue,
} from "@/types/notification";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  NotificationListFilters,
  NotificationService,
} from "@/services/interfaces/notificationService";

function mapNotification(raw: Record<string, unknown>): AppNotification {
  return {
    id: String(raw.id),
    title: String(raw.title),
    message: String(raw.message),
    type: (raw.type as NotificationTypeValue) ?? "info",
    category: (raw.category as NotificationCategoryValue) ?? "system",
    isRead: Boolean(raw.isRead),
    actionUrl: raw.actionUrl as string | undefined,
    entityType: raw.entityType as string | undefined,
    entityId: raw.entityId as string | undefined,
    recipientId: String(raw.recipientId),
    createdAt: String(raw.createdAt ?? raw.createdOnUtc ?? new Date().toISOString()),
    readAt: raw.readAt as string | undefined,
  };
}

function mapPage(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<AppNotification> {
  return {
    items: (raw.items ?? []).map((item) => mapNotification(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpNotificationService: NotificationService = {
  async list(filters: NotificationListFilters) {
    return mapPage(
      await apiRequest(`/api/notifications${buildQuery(filters as Record<string, unknown>)}`),
    );
  },

  async getById(id: string) {
    return mapNotification(
      await apiRequest<Record<string, unknown>>(`/api/notifications/${id}`),
    );
  },

  async markAsRead(id: string) {
    return mapNotification(
      await apiRequest<Record<string, unknown>>(`/api/notifications/${id}/read`, {
        method: "POST",
      }),
    );
  },

  async markAllAsRead(recipientId: string) {
    await apiRequest(`/api/notifications/mark-all-read`, {
      method: "POST",
      body: JSON.stringify({ recipientId }),
    });
  },

  async getUnreadCount(recipientId: string) {
    const raw = await apiRequest<{ count?: number }>(
      `/api/notifications/unread-count${buildQuery({ recipientId })}`,
    );
    return Number(raw.count ?? 0);
  },

  async delete(id: string) {
    await apiRequest(`/api/notifications/${id}`, { method: "DELETE" });
  },
};
