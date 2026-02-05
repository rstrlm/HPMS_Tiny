import { fetchJson } from "./client";
import type {
  CleaningTaskDto,
  CleaningTaskSummaryDto,
  CreateCleaningTaskRequest,
  UpdateCleaningTaskRequest,
  AssignTaskRequest,
  SkipTaskRequest
} from "./types";

export const getCleaningTasks = async (
  date?: string,
  status?: number,
  assignedToStaffId?: string
): Promise<CleaningTaskDto[]> => {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  if (status !== undefined) params.set("status", String(status));
  if (assignedToStaffId) params.set("assignedToStaffId", assignedToStaffId);
  const query = params.toString();
  return fetchJson<CleaningTaskDto[]>(query ? `/housekeeping/tasks?${query}` : "/housekeeping/tasks");
};

export const getCleaningTask = async (id: string): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}`);
};

export const getCleaningSummary = async (date?: string): Promise<CleaningTaskSummaryDto> => {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  const query = params.toString();
  return fetchJson<CleaningTaskSummaryDto>(query ? `/housekeeping/summary?${query}` : "/housekeeping/summary");
};

export const createCleaningTask = async (request: CreateCleaningTaskRequest): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>("/housekeeping/tasks", {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const generateDailyTasks = async (date?: string): Promise<CleaningTaskDto[]> => {
  const params = new URLSearchParams();
  if (date) params.set("date", date);
  const query = params.toString();
  return fetchJson<CleaningTaskDto[]>(
    query ? `/housekeeping/tasks/generate?${query}` : "/housekeeping/tasks/generate",
    { method: "POST" }
  );
};

export const updateCleaningTask = async (
  id: string,
  request: UpdateCleaningTaskRequest
): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}`, {
    method: "PATCH",
    body: JSON.stringify(request)
  });
};

export const assignTask = async (id: string, request: AssignTaskRequest): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}/assign`, {
    method: "POST",
    body: JSON.stringify(request)
  });
};

export const startTask = async (id: string): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}/start`, {
    method: "POST"
  });
};

export const completeTask = async (id: string): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}/complete`, {
    method: "POST"
  });
};

export const skipTask = async (id: string, request?: SkipTaskRequest): Promise<CleaningTaskDto> => {
  return fetchJson<CleaningTaskDto>(`/housekeeping/tasks/${id}/skip`, {
    method: "POST",
    body: request ? JSON.stringify(request) : undefined
  });
};
