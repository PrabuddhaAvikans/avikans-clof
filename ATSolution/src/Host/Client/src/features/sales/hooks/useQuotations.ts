import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  quotationService,
  type QuotationContactInput,
  type QuotationFormData,
  type QuotationListFilters,
} from "@/services";

export const quotationKeys = {
  all: ["quotations"] as const,
  lists: () => [...quotationKeys.all, "list"] as const,
  list: (filters: QuotationListFilters) => [...quotationKeys.lists(), filters] as const,
  details: () => [...quotationKeys.all, "detail"] as const,
  detail: (id: string) => [...quotationKeys.details(), id] as const,
};

export function useQuotations(filters: QuotationListFilters) {
  return useQuery({
    queryKey: quotationKeys.list(filters),
    queryFn: () => quotationService.list(filters),
  });
}

export function useQuotation(id: string) {
  return useQuery({
    queryKey: quotationKeys.detail(id),
    queryFn: () => quotationService.getById(id),
    enabled: Boolean(id),
  });
}

export function useCreateQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: QuotationFormData) => quotationService.create(data),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.all });
    },
  });
}

export function useUpdateQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: Partial<QuotationFormData> }) =>
      quotationService.update(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: quotationKeys.lists() });
    },
  });
}

export function useSendQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotationService.send(id),
    onSuccess: (_, id) => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: quotationKeys.lists() });
    },
  });
}

export function useConvertQuotationToSalesOrder() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotationService.convertToSalesOrder(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.all });
      void queryClient.invalidateQueries({ queryKey: ["salesOrders"] });
    },
  });
}

export function useDeleteQuotation() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => quotationService.delete(id),
    onSuccess: () => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.all });
    },
  });
}

export function useAddQuotationContact() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: QuotationContactInput }) =>
      quotationService.addContactEntry(id, data),
    onSuccess: (_, { id }) => {
      void queryClient.invalidateQueries({ queryKey: quotationKeys.detail(id) });
      void queryClient.invalidateQueries({ queryKey: quotationKeys.lists() });
    },
  });
}
