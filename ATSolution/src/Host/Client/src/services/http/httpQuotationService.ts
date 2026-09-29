import type { Attachment, PaginatedResponse } from "@/types/common";
import type { Product } from "@/types/product";
import type { Quotation } from "@/types/quotation";
import type { SalesOrder } from "@/types/sales-order";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  QuotationContactInput,
  QuotationFormData,
  QuotationListFilters,
  QuotationService,
} from "@/services/interfaces/quotationService";

function mapQuotation(raw: Record<string, unknown>): Quotation {
  return {
    id: String(raw.id),
    quotationNumber: String(raw.quotationNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    customerEmail: String(raw.customerEmail ?? ""),
    status: (raw.status as Quotation["status"]) ?? "draft",
    priority: (raw.priority as Quotation["priority"]) ?? "medium",
    lineItems: (raw.lineItems as Quotation["lineItems"]) ?? [],
    subtotal: Number(raw.subtotal ?? 0),
    discountAmount: Number(raw.discountAmount ?? 0),
    taxAmount: Number(raw.taxAmount ?? 0),
    totalAmount: Number(raw.totalAmount ?? 0),
    currency: String(raw.currency ?? "LKR"),
    validUntil: String(raw.validUntil ?? new Date().toISOString()),
    paymentStatus: (raw.paymentStatus as Quotation["paymentStatus"]) ?? "unpaid",
    billingAddress: (raw.billingAddress as Quotation["billingAddress"]) ?? {
      line1: "",
      city: "",
      state: "",
      postalCode: "",
      country: "",
    },
    shippingAddress: raw.shippingAddress as Quotation["shippingAddress"],
    notes: raw.notes as string | undefined,
    termsAndConditions: raw.termsAndConditions as string | undefined,
    attachments: (raw.attachments as Attachment[]) ?? [],
    salesOrderId: raw.salesOrderId == null ? undefined : String(raw.salesOrderId),
    contactHistory: (raw.contactHistory as Quotation["contactHistory"]) ?? [],
    revisions: (raw.revisions as Quotation["revisions"]) ?? [],
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    sentAt: raw.sentAt as string | undefined,
    viewedAt: raw.viewedAt as string | undefined,
    acceptedAt: raw.acceptedAt as string | undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapSalesOrder(raw: Record<string, unknown>): SalesOrder {
  return {
    id: String(raw.id),
    orderNumber: String(raw.orderNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    customerEmail: String(raw.customerEmail ?? ""),
    quotationId: raw.quotationId == null ? undefined : String(raw.quotationId),
    quotationNumber: raw.quotationNumber as string | undefined,
    costingRequestId: raw.costingRequestId == null ? undefined : String(raw.costingRequestId),
    status: (raw.status as SalesOrder["status"]) ?? "draft",
    priority: (raw.priority as SalesOrder["priority"]) ?? "medium",
    lineItems: (raw.lineItems as SalesOrder["lineItems"]) ?? [],
    subtotal: Number(raw.subtotal ?? 0),
    discountAmount: Number(raw.discountAmount ?? 0),
    taxAmount: Number(raw.taxAmount ?? 0),
    totalAmount: Number(raw.totalAmount ?? 0),
    currency: String(raw.currency ?? "LKR"),
    paymentStatus: (raw.paymentStatus as SalesOrder["paymentStatus"]) ?? "unpaid",
    billingAddress: (raw.billingAddress as SalesOrder["billingAddress"]) ?? {
      line1: "",
      city: "",
      state: "",
      postalCode: "",
      country: "",
    },
    shippingAddress: raw.shippingAddress as SalesOrder["shippingAddress"],
    requestedDeliveryDate: raw.requestedDeliveryDate as string | undefined,
    notes: raw.notes as string | undefined,
    assignedTo: raw.assignedTo == null ? undefined : String(raw.assignedTo),
    assignedToName: raw.assignedToName as string | undefined,
    manufacturingJobIds: ((raw.manufacturingJobIds as string[]) ?? []).map(String),
    deliveryIds: ((raw.deliveryIds as string[]) ?? []).map(String),
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    confirmedAt: raw.confirmedAt as string | undefined,
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage<T>(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
  mapItem: (item: Record<string, unknown>) => T,
): PaginatedResponse<T> {
  return {
    items: (raw.items ?? []).map((item) => mapItem(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpQuotationService: QuotationService = {
  async list(filters: QuotationListFilters) {
    return mapPage(
      await apiRequest(`/api/quotations${buildQuery(filters as unknown as Record<string, unknown>)}`),
      mapQuotation,
    );
  },
  async getById(id: string) {
    return mapQuotation(await apiRequest(`/api/quotations/${id}`));
  },
  async create(data: QuotationFormData) {
    return mapQuotation(
      await apiRequest("/api/quotations", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<QuotationFormData>) {
    return mapQuotation(
      await apiRequest(`/api/quotations/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/quotations/${id}`, { method: "DELETE" });
  },
  async send(id: string) {
    return mapQuotation(await apiRequest(`/api/quotations/${id}/send`, { method: "POST" }));
  },
  async convertToSalesOrder(id: string) {
    return mapSalesOrder(
      await apiRequest(`/api/quotations/${id}/convert-to-sales-order`, { method: "POST" }),
    );
  },
  async addContactEntry(id: string, data: QuotationContactInput) {
    return mapQuotation(
      await apiRequest(`/api/quotations/${id}/contacts`, {
        method: "POST",
        body: JSON.stringify(data),
      }),
    );
  },
  async approveLineCustomization(quotationId: string, lineItemId: string, notes?: string) {
    return mapQuotation(
      await apiRequest(`/api/quotations/${quotationId}/lines/${lineItemId}/approve-customization`, {
        method: "POST",
        body: JSON.stringify({ notes }),
      }),
    );
  },
  async promoteCustomizationToProductVersion(
    quotationId: string,
    lineItemId: string,
    revisionNotes?: string,
  ) {
    const raw = await apiRequest<Record<string, unknown>>(
      `/api/quotations/${quotationId}/lines/${lineItemId}/promote-customization`,
      {
        method: "POST",
        body: JSON.stringify({ revisionNotes }),
      },
    );
    return {
      quotation: mapQuotation(raw.quotation as Record<string, unknown>),
      product: raw.product as Product,
    };
  },
};
