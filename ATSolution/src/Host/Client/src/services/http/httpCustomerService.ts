import type { PaginatedResponse } from "@/types/common";
import type { Customer } from "@/types/customer";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  CustomerFormData,
  CustomerListFilters,
  CustomerService,
} from "@/services/interfaces/customerService";

function mapCustomer(raw: Record<string, unknown>): Customer {
  return {
    id: String(raw.id),
    code: String(raw.code),
    name: String(raw.name),
    type: (raw.type as Customer["type"]) ?? "corporate",
    email: String(raw.email ?? ""),
    phone: String(raw.phone ?? ""),
    billingAddresses: (raw.billingAddresses as Customer["billingAddresses"]) ?? [],
    activeBillingAddressIndex: Number(raw.activeBillingAddressIndex ?? 0),
    deliverySameAsBilling: Boolean(raw.deliverySameAsBilling ?? true),
    shippingAddresses: raw.shippingAddresses as Customer["shippingAddresses"],
    activeShippingAddressIndex: raw.activeShippingAddressIndex as number | undefined,
    contactPersons: (raw.contactPersons as Customer["contactPersons"]) ?? [],
    taxId: raw.taxId as string | undefined,
    creditLimit: raw.creditLimit as number | undefined,
    paymentTermsDays: Number(raw.paymentTermsDays ?? 0),
    notes: raw.notes as string | undefined,
    status: (raw.status as Customer["status"]) ?? "active",
    totalOrders: Number(raw.totalOrders ?? 0),
    totalRevenue: Number(raw.totalRevenue ?? 0),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<Customer> {
  return {
    items: (raw.items ?? []).map((item) => mapCustomer(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpCustomerService: CustomerService = {
  async list(filters: CustomerListFilters) {
    return mapPage(
      await apiRequest(`/api/customers${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapCustomer(await apiRequest(`/api/customers/${id}`));
  },
  async create(data: CustomerFormData) {
    return mapCustomer(
      await apiRequest("/api/customers", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<CustomerFormData>) {
    return mapCustomer(
      await apiRequest(`/api/customers/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/customers/${id}`, { method: "DELETE" });
  },
};
