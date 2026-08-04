import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  costingService,
  type CostingListFilters,
} from "@/services";

export const costingKeys = {
  all: ["costing"] as const,
  lists: () => [...costingKeys.all, "list"] as const,
  list: (filters: CostingListFilters) => [...costingKeys.lists(), filters] as const,
  details: () => [...costingKeys.all, "detail"] as const,
  detail: (id: string) => [...costingKeys.details(), id] as const,
};

export function useCostingRequests(filters: CostingListFilters) {
  return useQuery({
    queryKey: costingKeys.list(filters),
    queryFn: () => costingService.list(filters),
  });
}

export function useCostingRequest(id: string) {
  return useQuery({
    queryKey: costingKeys.detail(id),
    queryFn: () => costingService.getById(id),
    enabled: Boolean(id),
  });
}

export function useApproveCostingRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      costingService.approve(id, comment),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: costingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: costingKeys.lists() });
    },
  });
}

export function useRejectCostingRequest() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment: string }) =>
      costingService.reject(id, comment),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: costingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: costingKeys.lists() });
    },
  });
}

export function useRequestCostingChanges() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment: string }) =>
      costingService.requestChanges(id, comment),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: costingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: costingKeys.lists() });
    },
  });
}

export function useUpdateCostingNotes() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, notes }: { id: string; notes: string }) =>
      costingService.updateNotes(id, notes),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: costingKeys.detail(id) });
    },
  });
}

export function useAddCostingComment() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment: string }) =>
      costingService.addComment(id, comment),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: costingKeys.detail(id) });
    },
  });
}
