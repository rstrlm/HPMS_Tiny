import { fetchJson } from "./client";
import type {
  StaffProfileDto,
  CreateStaffProfileRequest,
  UpdateStaffProfileRequest,
  CreateStaffWithKeycloakRequest
} from "./types";

export const getStaff = async (activeOnly?: boolean, search?: string): Promise<StaffProfileDto[]> => {
  const params = new URLSearchParams();
  if (activeOnly !== undefined) params.set("activeOnly", String(activeOnly));
  if (search) params.set("search", search);
  const query = params.toString();
  return fetchJson<StaffProfileDto[]>(query ? `/staff?${query}` : "/staff");
};

export const getStaffById = async (id: string): Promise<StaffProfileDto> => {
  return fetchJson<StaffProfileDto>(`/staff/${id}`);
};

export const getStaffByKeycloakUserId = async (keycloakUserId: string): Promise<StaffProfileDto> => {
  return fetchJson<StaffProfileDto>(`/staff/by-keycloak/${encodeURIComponent(keycloakUserId)}`);
};

export const createStaff = async (request: CreateStaffProfileRequest): Promise<StaffProfileDto> => {
  return fetchJson<StaffProfileDto>("/staff", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateStaff = async (
  id: string,
  request: UpdateStaffProfileRequest
): Promise<StaffProfileDto> => {
  return fetchJson<StaffProfileDto>(`/staff/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const deleteStaff = async (id: string): Promise<void> => {
  await fetchJson(`/staff/${id}`, { method: "DELETE" });
};

export const createStaffWithKeycloak = async (
  request: CreateStaffWithKeycloakRequest
): Promise<StaffProfileDto> => {
  return fetchJson<StaffProfileDto>("/staff/with-keycloak", {
    method: "POST",
    body: JSON.stringify(request)
  });
};
