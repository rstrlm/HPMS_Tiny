import { buildApiUrl } from "../lib/config";
import { getAccessToken } from "../state/auth";

export type ApiError = {
  status: number;
  message: string;
};

export const fetchJson = async <T>(path: string, init?: RequestInit): Promise<T> => {
  const token = getAccessToken();
  const response = await fetch(buildApiUrl(path), {
    ...init,
    headers: {
      "Content-Type": "application/json",
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init?.headers
    }
  });

  if (!response.ok) {
    const message = await response.text();
    throw { status: response.status, message } as ApiError;
  }

  return (await response.json()) as T;
};
