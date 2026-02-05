import { fetchJson } from "./client";
import type {
  RoomDto,
  RoomStateBlockDto,
  RoomTypeDto,
  CreateRoomRequest,
  UpdateRoomRequest,
  CreateRoomTypeRequest,
  UpdateRoomTypeRequest
} from "./types";

// Rooms
export const getRooms = async (activeOnly?: boolean): Promise<RoomDto[]> => {
  const params = new URLSearchParams();
  if (activeOnly !== undefined) {
    params.set("activeOnly", String(activeOnly));
  }
  const query = params.toString();
  return fetchJson<RoomDto[]>(query ? `/rooms?${query}` : "/rooms");
};

export const getRoom = async (id: string): Promise<RoomDto> => {
  return fetchJson<RoomDto>(`/rooms/${id}`);
};

export const createRoom = async (request: CreateRoomRequest): Promise<RoomDto> => {
  return fetchJson<RoomDto>("/rooms", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateRoom = async (id: string, request: UpdateRoomRequest): Promise<RoomDto> => {
  return fetchJson<RoomDto>(`/rooms/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const deleteRoom = async (id: string): Promise<void> => {
  await fetchJson(`/rooms/${id}`, { method: "DELETE" });
};

// Room State Blocks
export const getRoomBlocks = async (
  roomId: string,
  from?: string,
  to?: string
): Promise<RoomStateBlockDto[]> => {
  const params = new URLSearchParams();
  if (from) params.set("from", from);
  if (to) params.set("to", to);
  const query = params.toString();
  return fetchJson<RoomStateBlockDto[]>(
    query ? `/rooms/${roomId}/blocks?${query}` : `/rooms/${roomId}/blocks`
  );
};

// Room Types
export const getRoomTypes = async (): Promise<RoomTypeDto[]> => {
  return fetchJson<RoomTypeDto[]>("/roomtypes");
};

export const getRoomType = async (id: string): Promise<RoomTypeDto> => {
  return fetchJson<RoomTypeDto>(`/roomtypes/${id}`);
};

export const createRoomType = async (request: CreateRoomTypeRequest): Promise<RoomTypeDto> => {
  return fetchJson<RoomTypeDto>("/roomtypes", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const updateRoomType = async (
  id: string,
  request: UpdateRoomTypeRequest
): Promise<RoomTypeDto> => {
  return fetchJson<RoomTypeDto>(`/roomtypes/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const deleteRoomType = async (id: string): Promise<void> => {
  await fetchJson(`/roomtypes/${id}`, { method: "DELETE" });
};
