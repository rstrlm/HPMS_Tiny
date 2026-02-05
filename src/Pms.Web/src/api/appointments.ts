import { fetchJson } from "./client";
import type {
  AppointmentDto,
  CreateAppointmentRequest,
  UpdateAppointmentRequest,
  UpdateAppointmentStatusRequest,
  TreatmentTypeDto,
  CreateTreatmentTypeRequest,
  TreatmentRoomDto,
  CreateTreatmentRoomRequest,
  TimeSlotDto
} from "./types";

// Appointments
export const getAppointments = async (
  from?: string,
  to?: string,
  therapistId?: string
): Promise<AppointmentDto[]> => {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  if (therapistId) params.set("therapistId", therapistId);
  const query = params.toString();
  return fetchJson<AppointmentDto[]>(query ? `/appointments?${query}` : "/appointments");
};

export const getAppointment = async (id: string): Promise<AppointmentDto> => {
  return fetchJson<AppointmentDto>(`/appointments/${id}`);
};

export const createAppointment = async (request: CreateAppointmentRequest): Promise<AppointmentDto> => {
  return fetchJson<AppointmentDto>("/appointments", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateAppointment = async (
  id: string,
  request: UpdateAppointmentRequest
): Promise<AppointmentDto> => {
  return fetchJson<AppointmentDto>(`/appointments/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const updateAppointmentStatus = async (
  id: string,
  request: UpdateAppointmentStatusRequest
): Promise<AppointmentDto> => {
  return fetchJson<AppointmentDto>(`/appointments/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const deleteAppointment = async (id: string): Promise<void> => {
  await fetchJson<void>(`/appointments/${id}`, {
    method: "DELETE"
  });
};

// Treatment Types
export const getTreatmentTypes = async (activeOnly?: boolean): Promise<TreatmentTypeDto[]> => {
  const params = new URLSearchParams();
  if (activeOnly !== undefined) params.set("activeOnly", String(activeOnly));
  const query = params.toString();
  return fetchJson<TreatmentTypeDto[]>(query ? `/treatments/types?${query}` : "/treatments/types");
};

export const getTreatmentType = async (id: string): Promise<TreatmentTypeDto> => {
  return fetchJson<TreatmentTypeDto>(`/treatments/types/${id}`);
};

export const createTreatmentType = async (
  request: CreateTreatmentTypeRequest
): Promise<TreatmentTypeDto> => {
  return fetchJson<TreatmentTypeDto>("/treatments/types", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

// Treatment Rooms
export const getTreatmentRooms = async (activeOnly?: boolean): Promise<TreatmentRoomDto[]> => {
  const params = new URLSearchParams();
  if (activeOnly !== undefined) params.set("activeOnly", String(activeOnly));
  const query = params.toString();
  return fetchJson<TreatmentRoomDto[]>(query ? `/treatmentRooms?${query}` : "/treatmentRooms");
};

export const getTreatmentRoom = async (id: string): Promise<TreatmentRoomDto> => {
  return fetchJson<TreatmentRoomDto>(`/treatmentRooms/${id}`);
};

export const createTreatmentRoom = async (
  request: CreateTreatmentRoomRequest
): Promise<TreatmentRoomDto> => {
  return fetchJson<TreatmentRoomDto>("/treatmentRooms", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const getTreatmentRoomAvailability = async (
  id: string,
  date: string,
  durationMinutes: number,
  seats?: number
): Promise<TimeSlotDto[]> => {
  const params = new URLSearchParams();
  params.set("date", date);
  params.set("durationMinutes", String(durationMinutes));
  if (seats !== undefined) params.set("seats", String(seats));
  return fetchJson<TimeSlotDto[]>(`/treatmentRooms/${id}/availability?${params.toString()}`);
};
