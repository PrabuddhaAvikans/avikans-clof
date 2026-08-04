import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  deliveryService,
  type DeliveryFormData,
  type DeliveryListFilters,
} from "@/services";
import type { ProofOfDelivery } from "@/types/delivery";

export const deliveryKeys = {
  all: ["deliveries"] as const,
  lists: () => [...deliveryKeys.all, "list"] as const,
  list: (filters: DeliveryListFilters) => [...deliveryKeys.lists(), filters] as const,
  details: () => [...deliveryKeys.all, "detail"] as const,
  detail: (id: string) => [...deliveryKeys.details(), id] as const,
};

export function useDeliveries(filters: DeliveryListFilters) {
  return useQuery({
    queryKey: deliveryKeys.list(filters),
    queryFn: () => deliveryService.list(filters),
  });
}

export function useDelivery(id: string) {
  return useQuery({
    queryKey: deliveryKeys.detail(id),
    queryFn: () => deliveryService.getById(id),
    enabled: Boolean(id),
  });
}

export function useCreateDelivery() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: DeliveryFormData) => deliveryService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.all });
    },
  });
}

export function useUpdateDelivery() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<DeliveryFormData> }) =>
      deliveryService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() });
    },
  });
}

export function useDispatchDelivery() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deliveryService.dispatchDelivery(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() });
    },
  });
}

export function useRecordProofOfDelivery() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, proof }: { id: string; proof: Omit<ProofOfDelivery, "id"> }) =>
      deliveryService.recordProofOfDelivery(id, proof),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: deliveryKeys.lists() });
    },
  });
}
