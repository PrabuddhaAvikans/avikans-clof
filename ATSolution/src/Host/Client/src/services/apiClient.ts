import type { ApiError, PaginatedRequest } from "@/types/common";

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

export function buildQuery(params: Record<string, unknown> | PaginatedRequest): string {
  const search = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === "") continue;
    search.set(key, String(value));
  }
  const qs = search.toString();
  return qs ? `?${qs}` : "";
}

function toApiError(payload: unknown, status: number, traceId?: string | null): ApiError {
  if (payload && typeof payload === "object") {
    const obj = payload as Record<string, unknown>;
    if (typeof obj.message === "string") {
      return {
        code: typeof obj.code === "string" ? obj.code : `HTTP_${status}`,
        message: obj.message,
        details: obj.details as Record<string, string[]> | undefined,
        traceId: (obj.traceId as string | undefined) ?? traceId ?? undefined,
      };
    }
    if (typeof obj.title === "string") {
      return {
        code: typeof obj.type === "string" ? obj.type : `HTTP_${status}`,
        message: obj.title,
        details: obj.errors as Record<string, string[]> | undefined,
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
  const token = getAuthToken();
  if (token) {
    headers.set("Authorization", `Bearer ${token}`);
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
