import type { CleaningTaskStatus, CleaningTaskType, AppointmentStatus } from "../api/types";

// Cleaning Task Status
export const CLEANING_TASK_STATUS_LABELS: Record<string, string> = {
  "0": "Pending",
  "1": "InProgress",
  "2": "Completed",
  "3": "Skipped"
};

export const getCleaningTaskStatusLabel = (status: CleaningTaskStatus) => {
  if (typeof status === "number") return CLEANING_TASK_STATUS_LABELS[String(status)] ?? "Unknown";
  if (!status) return "Unknown";
  return CLEANING_TASK_STATUS_LABELS[status] ?? status;
};

// Cleaning Task Type
export const CLEANING_TASK_TYPE_LABELS: Record<string, string> = {
  "0": "Checkout",
  "1": "Stayover",
  "2": "Inspection"
};

export const getCleaningTaskTypeLabel = (type: CleaningTaskType) => {
  if (typeof type === "number") return CLEANING_TASK_TYPE_LABELS[String(type)] ?? "Unknown";
  if (!type) return "Unknown";
  return CLEANING_TASK_TYPE_LABELS[type] ?? type;
};

// Appointment Status
export const APPOINTMENT_STATUS_LABELS: Record<string, string> = {
  "0": "Pending",
  "1": "Confirmed",
  "2": "Completed",
  "3": "Cancelled"
};

export const getAppointmentStatusLabel = (status: AppointmentStatus) => {
  if (typeof status === "number") return APPOINTMENT_STATUS_LABELS[String(status)] ?? "Unknown";
  if (!status) return "Unknown";
  return APPOINTMENT_STATUS_LABELS[status] ?? status;
};
