import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  notificationService,
  type NotificationListFilters,
} from "@/services";

export const notificationKeys = {
  all: ["notifications"] as const,
  lists: () => [...notificationKeys.all, "list"] as const,
  list: (filters: NotificationListFilters) =>
    [...notificationKeys.lists(), filters] as const,
  unreadCount: (recipientId: string) =>
    [...notificationKeys.all, "unread-count", recipientId] as const,
  details: () => [...notificationKeys.all, "detail"] as const,
  detail: (id: string) => [...notificationKeys.details(), id] as const,
};

export function useNotifications(filters: NotificationListFilters) {
  return useQuery({
    queryKey: notificationKeys.list(filters),
    queryFn: () => notificationService.list(filters),
  });
}

export function useUnreadNotificationCount(recipientId: string) {
  return useQuery({
    queryKey: notificationKeys.unreadCount(recipientId),
    queryFn: () => notificationService.getUnreadCount(recipientId),
    enabled: Boolean(recipientId),
  });
}

export function useMarkNotificationAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => notificationService.markAsRead(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}

export function useMarkAllNotificationsAsRead() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (recipientId: string) => notificationService.markAllAsRead(recipientId),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: notificationKeys.all });
    },
  });
}
