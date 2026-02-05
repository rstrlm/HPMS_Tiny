import { buildApiUrl } from "../lib/config";
import { fetchJson } from "./client";
import type { BrandingDto, BrandingChangeLogDto, UpdateBrandingRequest } from "./types";

export type BrandingResponse = {
  companyName: string;
  tagline: string;
};

// Public endpoint (no auth required)
export const getBranding = async (): Promise<BrandingResponse> => {
  const response = await fetch(buildApiUrl("/branding"));

  if (!response.ok) {
    throw new Error("Failed to fetch branding");
  }

  return (await response.json()) as BrandingResponse;
};

// Manager-only: full branding settings
export const getBrandingFull = async (): Promise<BrandingDto> => {
  return fetchJson<BrandingDto>("/branding/full");
};

// Manager-only: update branding
export const updateBranding = async (request: UpdateBrandingRequest): Promise<BrandingDto> => {
  return fetchJson<BrandingDto>("/branding", {
    method: "PUT",
    body: JSON.stringify(request)
  });
};

// Manager-only: change history
export const getBrandingHistory = async (): Promise<BrandingChangeLogDto[]> => {
  return fetchJson<BrandingChangeLogDto[]>("/branding/history");
};
