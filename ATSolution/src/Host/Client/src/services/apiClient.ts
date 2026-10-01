import type { ApiError } from "@/types/common";

const configuredBase = (import.meta.env.VITE_API_BASE_URL as string | undefined)?.trim();
const API_BASE_URL = configuredBase ? configuredBase.replace(/\/$/, "") : "";

const AUTH_TOKEN_KEY = "avikans.auth.token";

export function getAuthToken(): string | null {
  if (typeof window === "undefined") return null;
  return (
    window.localStorage.getItem(AUTH_TOKEN_KEY) ??
    window.sessionStorage.getItem(AUTH_TOKEN_KEY)
  );
}

export function writeAuthToken(token: string, persist: boolean): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(AUTH_TOKEN_KEY);
  window.sessionStorage.removeItem(AUTH_TOKEN_KEY);
  const storage = persist ? window.localStorage : window.sessionStorage;
  storage.setItem(AUTH_TOKEN_KEY, token);
}

export function clearAuthToken(): void {
  if (typeof window === "undefined") return;
  window.localStorage.removeItem(AUTH_TOKEN_KEY);
  window.sessionStorage.removeItem(AUTH_TOKEN_KEY);
}

/** Serializes plain filter/query objects into `?key=value`. */
export function buildQuery(params: object): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params as Record<string, unknown>)) {
    if (value === undefined || value === null || value === "") continue;
    search.set(key, String(value));
  }
  const qs = search.toString();
  return qs ? `?${qs}` : "";
}

function toApiError(payload: unknown, status: number, traceId?: string | null): ApiError {
  if (payload && typeof payload === "object") {
    const obj = payload as Record<string, unknown>;
    const details =
      (obj.details as Record<string, string[]> | undefined) ??
      (obj.errors as Record<string, string[]> | undefined);

    const detailMessage = details
      ? Object.entries(details)
          .flatMap(([key, messages]) =>
            (messages ?? []).map((msg) =>
              key && key !== "" ? `${key}: ${msg}` : msg,
            ),
          )
          .filter(Boolean)
          .join("; ")
      : undefined;

    if (typeof obj.message === "string" && obj.message.trim()) {
      return {
        code: typeof obj.code === "string" ? obj.code : `HTTP_${status}`,
        message: detailMessage || obj.message,
        details,
        traceId: (obj.traceId as string | undefined) ?? traceId ?? undefined,
      };
    }
    if (typeof obj.title === "string") {
      return {
        code: typeof obj.type === "string" ? obj.type : `HTTP_${status}`,
        message: detailMessage || obj.title,
        details,
        traceId: traceId ?? undefined,
      };
    }
    if (detailMessage) {
      return {
        code: `HTTP_${status}`,
        message: detailMessage,
        details,
        traceId: traceId ?? undefined,
      };
    }
  }
  return {
    code: `HTTP_${status}`,
    message: status === 401 ? "Unauthorized." : "Request failed.",
    traceId: traceId ?? undefined,
  };
}

export async function apiRequest<T>(
  path: string,
  options: RequestInit = {},
): Promise<T> {
  const headers = new Headers(options.headers);
  if (!headers.has("Content-Type") && options.body) {
    headers.set("Content-Type", "application/json");
  }

  // Dev-only: auto-attach JWT so local API calls are authenticated.
  // Production builds must not rely on this helper for shipping auth headers
  // unless you intentionally add a production-safe path later.
  if (import.meta.env.DEV) {
    const token = getAuthToken();
    if (token) {
      headers.set("Authorization", `Bearer ${token}`);
    } else {
      headers.delete("Authorization");
    }
  }

  const response = await fetch(`${API_BASE_URL}${path.startsWith("/") ? path : `/${path}`}`, {
    ...options,
    headers,
  });

  const traceId = response.headers.get("trace-id");
  if (response.status === 204) {
    return undefined as T;
  }

  const text = await response.text();
  let payload: unknown = undefined;
  if (text) {
    try {
      payload = JSON.parse(text);
    } catch {
      payload = text;
    }
  }

  if (!response.ok) {
    throw toApiError(payload, response.status, traceId);
  }

  return payload as T;
}

function jsonBody(body: unknown): RequestInit {
  return {
    headers: { "Content-Type": "application/json" },
    // Drop nulls so optional JSON blobs (e.g. lineItems[].customization) are omitted
    // instead of sent as null — ASP.NET rejects null for JsonElement-like fields.
    body: JSON.stringify(body, (_key, value) => (value === null ? undefined : value)),
  };
}

/** Thin HTTP helpers for epic `execute` bodies. */
export const http = {
  get: <T = unknown>(path: string) => apiRequest<T>(path),
  post: <T = unknown>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: "POST",
      ...(body === undefined ? {} : jsonBody(body)),
    }),
  put: <T = unknown>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: "PUT",
      ...(body === undefined ? {} : jsonBody(body)),
    }),
  patch: <T = unknown>(path: string, body?: unknown) =>
    apiRequest<T>(path, {
      method: "PATCH",
      ...(body === undefined ? {} : jsonBody(body)),
    }),
  delete: <T = unknown>(path: string) =>
    apiRequest<T>(path, { method: "DELETE" }),
};
