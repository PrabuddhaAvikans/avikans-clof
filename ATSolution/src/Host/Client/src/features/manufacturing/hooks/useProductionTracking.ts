import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  productionTrackingService,
  type ProductionTrackingFilters,
} from "@/services";

export const productionTrackingKeys = {
  all: ["production-tracking"] as const,
  snapshot: () => [...productionTrackingKeys.all, "snapshot"] as const,
  lists: () => [...productionTrackingKeys.all, "list"] as const,
  list: (filters: ProductionTrackingFilters) =>
    [...productionTrackingKeys.lists(), filters] as const,
  details: () => [...productionTrackingKeys.all, "detail"] as const,
  detail: (id: string) => [...productionTrackingKeys.details(), id] as const,
};

export function useProductionSnapshot() {
  return useQuery({
    queryKey: productionTrackingKeys.snapshot(),
    queryFn: () => productionTrackingService.getSnapshot(),
  });
}

export function useProductionJobs(filters: ProductionTrackingFilters) {
  return useQuery({
    queryKey: productionTrackingKeys.list(filters),
    queryFn: () => productionTrackingService.listJobs(filters),
  });
}

export function useProductionJob(id: string) {
  return useQuery({
    queryKey: productionTrackingKeys.detail(id),
    queryFn: () => productionTrackingService.getJobById(id),
    enabled: Boolean(id),
  });
}

function invalidateAll(queryClient: ReturnType<typeof useQueryClient>) {
  void queryClient.invalidateQueries({ queryKey: productionTrackingKeys.all });
}

export function useStartProduction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (ids: string[]) => productionTrackingService.startProduction(ids),
    onSuccess: () => invalidateAll(queryClient),
  });
}

export function useUpdateProductionStage() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, comment }: { id: string; comment?: string }) =>
      productionTrackingService.updateStage(id, comment),
    onSuccess: () => invalidateAll(queryClient),
  });
}

export function useHoldProductionJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, reason }: { id: string; reason?: string }) =>
      productionTrackingService.holdJob(id, reason),
    onSuccess: () => invalidateAll(queryClient),
  });
}

export function useReleaseToQc() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => productionTrackingService.releaseToQc(id),
    onSuccess: () => invalidateAll(queryClient),
  });
}
