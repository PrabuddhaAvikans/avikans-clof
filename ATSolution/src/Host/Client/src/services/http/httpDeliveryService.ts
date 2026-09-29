import type { PaginatedResponse } from "@/types/common";
import type { Delivery, ProofOfDelivery } from "@/types/delivery";
import type { DeliveryStatusValue } from "@/types/status";
import { apiRequest, buildQuery } from "@/services/apiClient";
import type {
  DeliveryFormData,
  DeliveryListFilters,
  DeliveryService,
} from "@/services/interfaces/deliveryService";

function mapDeliveryItem(raw: Record<string, unknown>): Delivery["items"][number] {
  return {
    id: String(raw.id),
    productId: String(raw.productId),
    productSku: String(raw.productSku ?? ""),
    productName: String(raw.productName ?? ""),
    quantityOrdered: Number(raw.quantityOrdered ?? 0),
    quantityDelivered: Number(raw.quantityDelivered ?? 0),
    unit: String(raw.unit ?? "pcs"),
  };
}

function mapProof(raw: Record<string, unknown>): ProofOfDelivery {
  const gps = raw.gpsCoordinates as { lat?: number; lng?: number } | undefined;
  return {
    id: String(raw.id),
    signedBy: String(raw.signedBy ?? ""),
    signedAt: String(raw.signedAt ?? new Date().toISOString()),
    signatureUrl: raw.signatureUrl as string | undefined,
    photoUrls: ((raw.photoUrls as string[]) ?? []).map(String),
    notes: raw.notes as string | undefined,
    gpsCoordinates:
      gps && gps.lat != null && gps.lng != null
        ? { lat: Number(gps.lat), lng: Number(gps.lng) }
        : undefined,
  };
}

function mapDelivery(raw: Record<string, unknown>): Delivery {
  const shipping = (raw.shippingAddress as Record<string, unknown>) ?? {};
  return {
    id: String(raw.id),
    deliveryNumber: String(raw.deliveryNumber ?? ""),
    salesOrderId: String(raw.salesOrderId),
    salesOrderNumber: String(raw.salesOrderNumber ?? ""),
    customerId: String(raw.customerId),
    customerName: String(raw.customerName ?? ""),
    status: (raw.status as Delivery["status"]) ?? "planned",
    priority: (raw.priority as Delivery["priority"]) ?? "medium",
    items: ((raw.items as unknown[]) ?? []).map((item) =>
      mapDeliveryItem(item as Record<string, unknown>),
    ),
    shippingAddress: {
      line1: String(shipping.line1 ?? ""),
      line2: shipping.line2 as string | undefined,
      city: String(shipping.city ?? ""),
      state: String(shipping.state ?? ""),
      postalCode: String(shipping.postalCode ?? ""),
      country: String(shipping.country ?? ""),
    },
    carrier: raw.carrier as string | undefined,
    trackingNumber: raw.trackingNumber as string | undefined,
    driverId: raw.driverId == null ? undefined : String(raw.driverId),
    driverName: raw.driverName as string | undefined,
    vehicleNumber: raw.vehicleNumber as string | undefined,
    scheduledDate: String(raw.scheduledDate ?? new Date().toISOString()),
    dispatchedAt: raw.dispatchedAt as string | undefined,
    deliveredAt: raw.deliveredAt as string | undefined,
    proofOfDelivery: raw.proofOfDelivery
      ? mapProof(raw.proofOfDelivery as Record<string, unknown>)
      : undefined,
    notes: raw.notes as string | undefined,
    createdBy: String(raw.createdBy ?? ""),
    createdByName: String(raw.createdByName ?? ""),
    createdAt: String(raw.createdAt ?? new Date().toISOString()),
    updatedAt: String(raw.updatedAt ?? new Date().toISOString()),
  };
}

function mapPage(
  raw: { items?: unknown[]; totalCount?: number; page?: number; pageSize?: number; totalPages?: number },
): PaginatedResponse<Delivery> {
  return {
    items: (raw.items ?? []).map((item) => mapDelivery(item as Record<string, unknown>)),
    totalCount: raw.totalCount ?? 0,
    page: raw.page ?? 1,
    pageSize: raw.pageSize ?? 20,
    totalPages: raw.totalPages ?? 1,
  };
}

export const httpDeliveryService: DeliveryService = {
  async list(filters: DeliveryListFilters) {
    return mapPage(
      await apiRequest(`/api/deliveries${buildQuery(filters as unknown as Record<string, unknown>)}`),
    );
  },
  async getById(id: string) {
    return mapDelivery(await apiRequest(`/api/deliveries/${id}`));
  },
  async create(data: DeliveryFormData) {
    return mapDelivery(
      await apiRequest("/api/deliveries", { method: "POST", body: JSON.stringify(data) }),
    );
  },
  async update(id: string, data: Partial<DeliveryFormData>) {
    return mapDelivery(
      await apiRequest(`/api/deliveries/${id}`, { method: "PUT", body: JSON.stringify(data) }),
    );
  },
  async delete(id: string) {
    await apiRequest(`/api/deliveries/${id}`, { method: "DELETE" });
  },
  async updateStatus(id: string, status: DeliveryStatusValue) {
    return mapDelivery(
      await apiRequest(`/api/deliveries/${id}/status`, {
        method: "POST",
        body: JSON.stringify({ status }),
      }),
    );
  },
  async dispatchDelivery(id: string) {
    return mapDelivery(await apiRequest(`/api/deliveries/${id}/dispatch`, { method: "POST" }));
  },
  async recordProofOfDelivery(id: string, proof: Omit<ProofOfDelivery, "id">) {
    return mapDelivery(
      await apiRequest(`/api/deliveries/${id}/proof-of-delivery`, {
        method: "POST",
        body: JSON.stringify(proof),
      }),
    );
  },
};
