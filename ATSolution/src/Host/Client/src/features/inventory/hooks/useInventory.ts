import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  inventoryService,
  type InventoryFormData,
  type InventoryListFilters,
  type StockMovementFilters,
} from "@/services";
import type { StockMovementTypeValue } from "@/types/inventory";

export const inventoryKeys = {
  all: ["inventory"] as const,
  lists: () => [...inventoryKeys.all, "list"] as const,
  list: (filters: InventoryListFilters) => [...inventoryKeys.lists(), filters] as const,
  lowStock: () => [...inventoryKeys.all, "low-stock"] as const,
  movements: (filters: StockMovementFilters) =>
    [...inventoryKeys.all, "movements", filters] as const,
  details: () => [...inventoryKeys.all, "detail"] as const,
  detail: (id: string) => [...inventoryKeys.details(), id] as const,
};

export function useInventoryItems(filters: InventoryListFilters) {
  return useQuery({
    queryKey: inventoryKeys.list(filters),
    queryFn: () => inventoryService.list(filters),
  });
}

export function useInventoryItem(id: string) {
  return useQuery({
    queryKey: inventoryKeys.detail(id),
    queryFn: () => inventoryService.getById(id),
    enabled: Boolean(id),
  });
}

export function useLowStockItems() {
  return useQuery({
    queryKey: inventoryKeys.lowStock(),
    queryFn: () => inventoryService.getLowStock(),
  });
}

export function useStockMovements(filters: StockMovementFilters) {
  return useQuery({
    queryKey: inventoryKeys.movements(filters),
    queryFn: () => inventoryService.listMovements(filters),
  });
}

export function useCreateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: InventoryFormData) => inventoryService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.all });
    },
  });
}

export function useUpdateInventoryItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<InventoryFormData> }) =>
      inventoryService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.all });
    },
  });
}

export function useRecordStockMovement() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      inventoryItemId,
      type,
      quantity,
      reference,
    }: {
      inventoryItemId: string;
      type: StockMovementTypeValue;
      quantity: number;
      reference?: { referenceType: string; referenceId: string; notes?: string };
    }) => inventoryService.recordMovement(inventoryItemId, type, quantity, reference),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: inventoryKeys.all });
    },
  });
}
