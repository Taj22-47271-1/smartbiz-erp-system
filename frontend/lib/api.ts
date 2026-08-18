export type ApiError = { message?: string };

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5000/api";

export function getToken() {
  if (typeof window === "undefined") return null;
  return localStorage.getItem("smartbiz_token");
}

export function getStoredUser() {
  if (typeof window === "undefined") return null;
  const raw = localStorage.getItem("smartbiz_user");
  return raw ? JSON.parse(raw) : null;
}

export function logout() {
  localStorage.removeItem("smartbiz_token");
  localStorage.removeItem("smartbiz_user");
  window.location.href = "/login";
}

export async function api<T>(path: string, options: RequestInit = {}): Promise<T> {
  const token = getToken();
  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(options.headers ?? {})
    },
    cache: "no-store"
  });

  if (res.status === 204) return undefined as T;

  const data = await res.json().catch(() => ({}));
  if (!res.ok) {
    const message = (data as ApiError).message ?? `Request failed (${res.status})`;
    throw new Error(message);
  }

  return data as T;
}
