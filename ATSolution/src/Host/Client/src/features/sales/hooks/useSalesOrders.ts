import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  salesOrderService,
  type SalesOrderFormData,
  type SalesOrderListFilters,
} from "@/services";

export const salesOrderKeys = {
  all: ["salesOrders"] as const,
  lists: () => [...salesOrderKeys.all, "list"] as const,
  list: (filters: SalesOrderListFilters) => [...salesOrderKeys.lists(), filters] as const,
  details: () => [...salesOrderKeys.all, "detail"] as const,
  detail: (id: string) => [...salesOrderKeys.details(), id] as const,
};

export function useSalesOrders(filters: SalesOrderListFilters) {
  return useQuery({
    queryKey: salesOrderKeys.list(filters),
    queryFn: () => salesOrderService.list(filters),
  });
}

export function useSalesOrder(id: string) {
  return useQuery({
    queryKey: salesOrderKeys.detail(id),
    queryFn: () => salesOrderService.getById(id),
    enabled: Boolean(id),
  });
}

export function useCreateSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: SalesOrderFormData) => salesOrderService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.all });
    },
  });
}

export function useUpdateSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<SalesOrderFormData> }) =>
      salesOrderService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() });
    },
  });
}

export function useConfirmSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => salesOrderService.confirm(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() });
    },
  });
}

export function useCancelSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      salesOrderService.cancel(id, reason),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() });
    },
  });
}

export function useDeleteSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => salesOrderService.delete(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.all });
    },
  });
}

export function useAssignSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, userId }: { id: string; userId: string }) =>
      salesOrderService.assign(id, userId),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: salesOrderKeys.lists() });
    },
  });
}
