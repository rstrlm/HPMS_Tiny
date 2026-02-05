export const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? "";
export const apiPrefix = "/api/v1";

export const buildApiUrl = (path: string) => {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${apiBaseUrl}${apiPrefix}${normalized}`;
};
