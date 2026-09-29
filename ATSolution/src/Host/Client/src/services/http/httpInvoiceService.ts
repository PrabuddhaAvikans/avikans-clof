import type { PaginatedResponse } from "@/types/common";
import type { Invoice, InvoiceLineItem } from "@/types/invoice";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  InvoiceFormData,
  InvoiceListFilters,
  InvoiceService,
  InvoiceUpdateData,
} from "@/services/interfaces/invoiceService";

function mapLineItem(raw: Record<string, unknown>): InvoiceLineItem {
  return {
    id: String(raw.id),
    productId: String(raw.productId),
    productSku: String(raw.productSku ?? ""),
    productName: String(raw.productName ?? ""),
    quantity: Number(raw.quantity ?? 0),
    unitPrice: Number(raw.unitPrice ?? 0),
    taxPercent: Number(raw.taxPercent ?? 0),
    lineTotal: Number(raw.lineTotal ?? 0),
  };
}

function mapInvoice(raw: Record<string, unknown>): Invoice {
  return {
    id: String(raw.id),
    invoiceNumber: String(raw.invoiceNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    customerEmail: String(raw.customerEmail ?? ""),
    salesOrderId: raw.salesOrderId == null ? undefined : String(raw.salesOrderId),
    salesOrderNumber: raw.salesOrderNumber as string | undefined,
    status: (raw.status as Invoice["status"]) ?? "draft",
    issueDate: String(raw.issueDate ?? new Date().toISOString()),
    dueDate: String(raw.dueDate ?? new Date().toISOString()),
    lineItems: ((raw.lineItems as unknown[]) ?? []).map((item) =>
      mapLineItem(item as Record<string, unknown>),
    ),
    subtotal: Number(raw.subtotal ?? 0),
    taxAmount: Number(raw.taxAmount ?? 0),
    totalAmount: Number(raw.totalAmount ?? 0),
    amountPaid: Number(raw.amountPaid ?? 0),
    amountCredited: Number(raw.amountCredited ?? 0),
    outstandingAmount: Number(raw.outstandingAmount ?? 0),
    currency: String(raw.currency ?? "LKR"),
    notes: raw.notes as string | undefined,
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage(raw: {
  items?: unknown[];
  totalCount?: number;
  page?: number;
  pageSize?: number;
  totalPages?: number;
}): PaginatedResponse<Invoice> {
  return {
    items: (raw.items ?? []).map((item) => mapInvoice(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpInvoiceService: InvoiceService = {
  async list(filters: InvoiceListFilters) {
    return mapPage(
      await apiRequest(`/api/invoices${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapInvoice(await apiRequest(`/api/invoices/${id}`));
  },
  async create(data: InvoiceFormData) {
    return mapInvoice(
      await apiRequest("/api/invoices", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: InvoiceUpdateData) {
    return mapInvoice(
      await apiRequest(`/api/invoices/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async issue(id: string) {
    return mapInvoice(await apiRequest(`/api/invoices/${id}/issue`, { method: "POST" }));
  },
  async void(id: string) {
    return mapInvoice(await apiRequest(`/api/invoices/${id}/void`, { method: "POST" }));
  },
  async recordPayment(id: string, amount: number) {
    return mapInvoice(
      await apiRequest(`/api/invoices/${id}/payments`, {
        method: "POST",
        body: JSON.stringify({ amount }),
      }),
    );
  },
};
