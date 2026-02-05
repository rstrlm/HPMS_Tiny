import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getCleaningTasks,
  getCleaningSummary,
  generateDailyTasks,
  createCleaningTask,
  assignTask,
  startTask,
  completeTask,
  skipTask
} from "../api/housekeeping";
import { getRooms } from "../api/rooms";
import { getStaff } from "../api/staff";
import type { CleaningTaskDto, CreateCleaningTaskRequest, AssignTaskRequest } from "../api/types";
import { getCleaningTaskStatusLabel, getCleaningTaskTypeLabel } from "../lib/status";
import { hasAnyRole, useAuth } from "../state/auth";

const getStatusBadgeStyle = (status: string) => {
  switch (status) {
    case "Pending":
      return "bg-amber-100 text-amber-700";
    case "InProgress":
      return "bg-blue-100 text-blue-700";
    case "Completed":
      return "bg-emerald-100 text-emerald-700";
    case "Skipped":
      return "bg-slate-100 text-slate-600";
    default:
      return "bg-slate-100 text-slate-600";
  }
};

const formatDateForInput = (date: Date) => {
  return date.toISOString().split("T")[0];
};

export default function Housekeeping() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const queryClient = useQueryClient();

  const [selectedDate, setSelectedDate] = useState(() => formatDateForInput(new Date()));
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);

  // Modal state
  const [showAddModal, setShowAddModal] = useState(false);
  const [formRoomId, setFormRoomId] = useState("");
  const [formTaskType, setFormTaskType] = useState(0);
  const [formNotes, setFormNotes] = useState("");

  // Assignment modal state
  const [assigningTaskId, setAssigningTaskId] = useState<string | null>(null);
  const [assignStaffId, setAssignStaffId] = useState("");

  const tasksQuery = useQuery({
    queryKey: ["cleaningTasks", { date: selectedDate, status: statusFilter }],
    queryFn: () => getCleaningTasks(selectedDate, statusFilter)
  });

  const summaryQuery = useQuery({
    queryKey: ["cleaningSummary", { date: selectedDate }],
    queryFn: () => getCleaningSummary(selectedDate)
  });

  const roomsQuery = useQuery({
    queryKey: ["rooms", { activeOnly: true }],
    queryFn: () => getRooms(true)
  });

  const staffQuery = useQuery({
    queryKey: ["staff", { activeOnly: true }],
    queryFn: () => getStaff(true)
  });

  const generateMutation = useMutation({
    mutationFn: () => generateDailyTasks(selectedDate),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
    }
  });

  const startMutation = useMutation({
    mutationFn: (id: string) => startTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
    }
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => completeTask(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
    }
  });

  const skipMutation = useMutation({
    mutationFn: (id: string) => skipTask(id, { reason: "Skipped via UI" }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
    }
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateCleaningTaskRequest) => createCleaningTask(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
      resetForm();
      setShowAddModal(false);
    }
  });

  const assignMutation = useMutation({
    mutationFn: ({ taskId, request }: { taskId: string; request: AssignTaskRequest }) =>
      assignTask(taskId, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningTasks"] });
      setAssigningTaskId(null);
      setAssignStaffId("");
    }
  });

  const resetForm = () => {
    setFormRoomId("");
    setFormTaskType(0);
    setFormNotes("");
  };

  const handleCreateTask = () => {
    if (!formRoomId) return;

    createMutation.mutate({
      roomId: formRoomId,
      taskType: formTaskType,
      scheduledDate: selectedDate,
      notes: formNotes || undefined
    });
  };

  const handleAssignTask = () => {
    if (!assigningTaskId || !assignStaffId) return;

    assignMutation.mutate({
      taskId: assigningTaskId,
      request: { staffId: assignStaffId }
    });
  };

  const tasks = tasksQuery.data ?? [];
  const rooms = roomsQuery.data ?? [];
  const cleaners = staffQuery.data?.filter((s) => s.skills?.toLowerCase().includes("cleaner")) ?? [];
  const summary = summaryQuery.data;

  const metrics = useMemo(() => {
    if (!summary) {
      return [
        { label: "Total", value: "—" },
        { label: "Pending", value: "—" },
        { label: "In Progress", value: "—" },
        { label: "Completed", value: "—" }
      ];
    }
    return [
      { label: "Total", value: String(summary.total) },
      { label: "Pending", value: String(summary.pending) },
      { label: "In Progress", value: String(summary.inProgress) },
      { label: "Completed", value: String(summary.completed) }
    ];
  }, [summary]);

  const handleTaskAction = (task: CleaningTaskDto) => {
    const status = getCleaningTaskStatusLabel(task.status);
    if (status === "Pending") {
      startMutation.mutate(task.id);
    } else if (status === "InProgress") {
      completeMutation.mutate(task.id);
    }
  };

  const getActionLabel = (task: CleaningTaskDto) => {
    const status = getCleaningTaskStatusLabel(task.status);
    if (status === "Pending") return "Start";
    if (status === "InProgress") return "Complete";
    return null;
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Housekeeping</p>
          <h2 className="text-2xl font-semibold text-slate-900">Cleaning tasks</h2>
          <p className="mt-1 text-sm text-slate-500">Daily task queue for cleaners.</p>
        </div>
        <div className="flex gap-2">
          {isManager && (
            <>
              <button
                onClick={() => setShowAddModal(true)}
                className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                + Add task
              </button>
              <button
                onClick={() => generateMutation.mutate()}
                disabled={generateMutation.isPending}
                className="rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {generateMutation.isPending ? "Generating..." : "Generate day list"}
              </button>
            </>
          )}
        </div>
      </header>

      <section className="grid gap-4 md:grid-cols-4">
        {metrics.map((metric) => (
          <div key={metric.label} className="panel px-4 py-5">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-400">{metric.label}</p>
            <p className="mt-3 text-3xl font-semibold text-slate-900">{metric.value}</p>
          </div>
        ))}
      </section>

      <section className="panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-900">Tasks</h3>
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
            />
            <select
              value={statusFilter ?? ""}
              onChange={(e) => setStatusFilter(e.target.value ? Number(e.target.value) : undefined)}
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
            >
              <option value="">All statuses</option>
              <option value="0">Pending</option>
              <option value="1">In Progress</option>
              <option value="2">Completed</option>
              <option value="3">Skipped</option>
            </select>
          </div>
        </div>
        <div className="mt-4 grid gap-3">
          {tasksQuery.isLoading && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
              Loading tasks...
            </div>
          )}
          {tasksQuery.isError && (
            <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-6 text-center text-sm text-rose-600">
              Failed to load tasks. Check your token or API.
            </div>
          )}
          {!tasksQuery.isLoading && !tasksQuery.isError && tasks.length === 0 && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
              No tasks for this date.{" "}
              {isManager && (
                <button
                  onClick={() => generateMutation.mutate()}
                  className="text-slate-900 underline"
                >
                  Generate tasks
                </button>
              )}
            </div>
          )}
          {tasks.map((task) => {
            const statusLabel = getCleaningTaskStatusLabel(task.status);
            const typeLabel = getCleaningTaskTypeLabel(task.taskType);
            const actionLabel = getActionLabel(task);
            const isActionPending =
              startMutation.isPending || completeMutation.isPending || skipMutation.isPending || assignMutation.isPending;

            return (
              <div
                key={task.id}
                className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-white px-4 py-4"
              >
                <div>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-400">
                    Room {task.roomNumber ?? "—"}
                  </p>
                  <p className="text-lg font-semibold text-slate-900">{typeLabel}</p>
                  <p className="text-sm text-slate-500">
                    {task.assignedToStaffName ? (
                      `Assigned to ${task.assignedToStaffName}`
                    ) : isManager && cleaners.length > 0 ? (
                      <button
                        onClick={() => setAssigningTaskId(task.id)}
                        className="text-slate-900 underline"
                      >
                        Assign cleaner
                      </button>
                    ) : (
                      "Unassigned"
                    )}
                  </p>
                </div>
                <div className="text-right">
                  <span className={`badge ${getStatusBadgeStyle(statusLabel)}`}>{statusLabel}</span>
                  {task.notes && (
                    <p className="mt-1 text-xs text-slate-500">{task.notes}</p>
                  )}
                </div>
                <div className="flex gap-2">
                  {actionLabel && (
                    <button
                      onClick={() => handleTaskAction(task)}
                      disabled={isActionPending}
                      className="rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                    >
                      {actionLabel}
                    </button>
                  )}
                  {statusLabel === "Pending" && (
                    <button
                      onClick={() => skipMutation.mutate(task.id)}
                      disabled={isActionPending}
                      className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 disabled:opacity-50"
                    >
                      Skip
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* Add Task Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">New</p>
              <h3 className="text-xl font-semibold text-slate-900">Add cleaning task</h3>
            </div>

            {rooms.length === 0 && (
              <div className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
                <p className="text-sm text-amber-700">No rooms available. Please create rooms first.</p>
              </div>
            )}

            <div className="space-y-4">
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Room *
                </label>
                <select
                  value={formRoomId}
                  onChange={(e) => setFormRoomId(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value="">Select room</option>
                  {rooms.map((r) => (
                    <option key={r.id} value={r.id}>
                      Room {r.roomNumber}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Task Type *
                </label>
                <select
                  value={formTaskType}
                  onChange={(e) => setFormTaskType(Number(e.target.value))}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value={0}>Checkout Cleaning</option>
                  <option value={1}>Stay-over Cleaning</option>
                  <option value={2}>Deep Clean</option>
                </select>
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Notes
                </label>
                <textarea
                  value={formNotes}
                  onChange={(e) => setFormNotes(e.target.value)}
                  rows={2}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  placeholder="Optional notes..."
                />
              </div>
            </div>

            {createMutation.isError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to create task.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => {
                  resetForm();
                  setShowAddModal(false);
                }}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleCreateTask}
                disabled={!formRoomId || createMutation.isPending}
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {createMutation.isPending ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Assign Task Modal */}
      {assigningTaskId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Assign</p>
              <h3 className="text-xl font-semibold text-slate-900">Assign cleaner</h3>
            </div>

            <div>
              <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                Cleaner *
              </label>
              <select
                value={assignStaffId}
                onChange={(e) => setAssignStaffId(e.target.value)}
                className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
              >
                <option value="">Select cleaner</option>
                {cleaners.map((c) => (
                  <option key={c.id} value={c.id}>
                    {c.displayName}
                  </option>
                ))}
              </select>
            </div>

            {assignMutation.isError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to assign task.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => {
                  setAssigningTaskId(null);
                  setAssignStaffId("");
                }}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleAssignTask}
                disabled={!assignStaffId || assignMutation.isPending}
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {assignMutation.isPending ? "Assigning..." : "Assign"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
