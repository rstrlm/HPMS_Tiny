import type { RoomStateBlockType, RoomStatus } from "../api/types";

export const ROOM_STATUS_LABELS: Record<string, string> = {
  "0": "Available",
  "1": "Occupied",
  "2": "NeedsCleaning",
  "3": "CleaningInProgress",
  "4": "OutOfService",
  "5": "Maintenance"
};

export const ROOM_STATUS_OPTIONS = [
  { value: 0, label: "Available" },
  { value: 1, label: "Occupied" },
  { value: 2, label: "Needs Cleaning" },
  { value: 3, label: "Cleaning In Progress" },
  { value: 4, label: "Out Of Service" },
  { value: 5, label: "Maintenance" }
];

export const getRoomStatusLabel = (status: RoomStatus) => {
  if (typeof status === "number") return ROOM_STATUS_LABELS[String(status)] ?? "Unknown";
  if (!status) return "Unknown";
  return ROOM_STATUS_LABELS[status] ?? status;
};

export const ROOM_BLOCK_LABELS: Record<string, string> = {
  "0": "Maintenance",
  "1": "OutOfService"
};

export const getRoomBlockLabel = (type: RoomStateBlockType) => {
  if (typeof type === "number") return ROOM_BLOCK_LABELS[String(type)] ?? "Unknown";
  if (!type) return "Unknown";
  return ROOM_BLOCK_LABELS[type] ?? type;
};
