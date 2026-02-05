import { fetchJson } from "./client";
import type {
  ReservationDto,
  CreateReservationRequest,
  UpdateReservationRequest,
  ChangeReservationStatusRequest,
  CreateRoomAssignmentRequest,
  RoomAssignmentDto,
  RoomAvailabilityInfo,
  PlaceHoldRequest,
  HoldDto
} from "./types";

export const getReservations = async (
  from?: string,
  to?: string,
  status?: number
): Promise<ReservationDto[]> => {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  if (status !== undefined) params.set("status", String(status));
  const query = params.toString();
  return fetchJson<ReservationDto[]>(query ? `/reservations?${query}` : "/reservations");
};

export const getReservation = async (id: string): Promise<ReservationDto> => {
  return fetchJson<ReservationDto>(`/reservations/${id}`);
};

export const createReservation = async (
  request: CreateReservationRequest
): Promise<ReservationDto> => {
  return fetchJson<ReservationDto>("/reservations", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateReservation = async (
  id: string,
  request: UpdateReservationRequest
): Promise<ReservationDto> => {
  return fetchJson<ReservationDto>(`/reservations/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const changeReservationStatus = async (
  id: string,
  request: ChangeReservationStatusRequest
): Promise<ReservationDto> => {
  return fetchJson<ReservationDto>(`/reservations/${id}/status`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const addRoomAssignment = async (
  reservationId: string,
  request: CreateRoomAssignmentRequest
): Promise<RoomAssignmentDto> => {
  return fetchJson<RoomAssignmentDto>(`/reservations/${reservationId}/assignments`, {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const getRoomAvailability = async (
  from: string,
  to: string,
  roomTypeId?: string
): Promise<RoomAvailabilityInfo[]> => {
  const params = new URLSearchParams();
  params.set("from", from);
  params.set("to", to);
  if (roomTypeId) params.set("roomTypeId", roomTypeId);
  return fetchJson<RoomAvailabilityInfo[]>(`/reservations/availability?${params.toString()}`);
};

export const placeHold = async (request: PlaceHoldRequest): Promise<HoldDto> => {
  return fetchJson<HoldDto>("/reservations/holds", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const releaseHold = async (holdId: string): Promise<void> => {
  await fetchJson(`/reservations/holds/${holdId}`, { method: "DELETE" });
};
