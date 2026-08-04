import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  manufacturingService,
  type ManufacturingJobFormData,
  type ManufacturingListFilters,
} from "@/services";

export const manufacturingKeys = {
  all: ["manufacturing"] as const,
  lists: () => [...manufacturingKeys.all, "list"] as const,
  list: (filters: ManufacturingListFilters) =>
    [...manufacturingKeys.lists(), filters] as const,
  details: () => [...manufacturingKeys.all, "detail"] as const,
  detail: (id: string) => [...manufacturingKeys.details(), id] as const,
};

export function useManufacturingJobs(filters: ManufacturingListFilters) {
  return useQuery({
    queryKey: manufacturingKeys.list(filters),
    queryFn: () => manufacturingService.list(filters),
  });
}

export function useManufacturingJob(id: string) {
  return useQuery({
    queryKey: manufacturingKeys.detail(id),
    queryFn: () => manufacturingService.getById(id),
    enabled: Boolean(id),
  });
}

export function useCreateManufacturingJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: ManufacturingJobFormData) => manufacturingService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.all });
    },
  });
}

export function useUpdateManufacturingJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<ManufacturingJobFormData> }) =>
      manufacturingService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.lists() });
    },
  });
}

export function useReserveMaterials() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => manufacturingService.reserveMaterials(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.lists() });
      void queryClient.invalidateQueries({ queryKey: ["inventory"] });
    },
  });
}

export function useStartManufacturingJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => manufacturingService.startJob(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.lists() });
    },
  });
}

export function useCompleteManufacturingJob() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => manufacturingService.completeJob(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: manufacturingKeys.lists() });
    },
  });
}
