import { useMemo } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import StatCard from "../components/StatCard";
import { hasAnyRole, useAuth } from "../state/auth";
import { getRooms } from "../api/rooms";
import { getCleaningSummary, generateDailyTasks } from "../api/housekeeping";
import { getAppointments } from "../api/appointments";
import { getRoomStatusLabel } from "../lib/room";

const formatDateForApi = (date: Date) => {
  return date.toISOString().split("T")[0];
};

export default function Dashboard() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const isCleaner = hasAnyRole(roles, ["cleaner"]);
  const isTherapist = hasAnyRole(roles, ["therapist"]);
  const queryClient = useQueryClient();

  const today = formatDateForApi(new Date());

  // Calculate today's date range for appointments
  const { from, to } = useMemo(() => {
    const startOfDay = new Date(`${today}T00:00:00`);
    const endOfDay = new Date(`${today}T23:59:59`);
    return {
      from: startOfDay.toISOString(),
      to: endOfDay.toISOString()
    };
  }, [today]);

  // Fetch rooms for occupancy stats
  const roomsQuery = useQuery({
    queryKey: ["rooms", { activeOnly: true }],
    queryFn: () => getRooms(true)
  });

  // Fetch cleaning summary for today
  const cleaningSummaryQuery = useQuery({
    queryKey: ["cleaningSummary", { date: today }],
    queryFn: () => getCleaningSummary(today),
    enabled: isManager || isCleaner
  });

  // Fetch today's appointments
  const appointmentsQuery = useQuery({
    queryKey: ["appointments", { from, to }],
    queryFn: () => getAppointments(from, to),
    enabled: isManager || isTherapist
  });

  // Generate cleaning tasks mutation
  const generateMutation = useMutation({
    mutationFn: () => generateDailyTasks(today),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["cleaningSummary"] });
    }
  });

  // Calculate room occupancy
  const roomStats = useMemo(() => {
    const rooms = roomsQuery.data ?? [];
    const occupied = rooms.filter((r) => getRoomStatusLabel(r.currentStatus) === "Occupied").length;
    const needsCleaning = rooms.filter((r) => getRoomStatusLabel(r.currentStatus) === "NeedsCleaning").length;
    return { total: rooms.length, occupied, needsCleaning };
  }, [roomsQuery.data]);

  // Cleaning stats
  const cleaningStats = cleaningSummaryQuery.data;

  // Appointment stats
  const appointmentStats = useMemo(() => {
    const appointments = appointmentsQuery.data ?? [];
    return { total: appointments.length };
  }, [appointmentsQuery.data]);

  // Build alerts
  const alerts = useMemo(() => {
    const items: { type: "warning" | "success"; message: string }[] = [];

    if (roomStats.needsCleaning > 0) {
      items.push({
        type: "warning",
        message: `${roomStats.needsCleaning} room${roomStats.needsCleaning > 1 ? "s" : ""} waiting for cleaning.`
      });
    }

    if (cleaningStats && cleaningStats.pending > 0) {
      items.push({
        type: "warning",
        message: `${cleaningStats.pending} cleaning task${cleaningStats.pending > 1 ? "s" : ""} pending.`
      });
    }

    if (cleaningStats && cleaningStats.total === 0) {
      items.push({
        type: "warning",
        message: "No cleaning tasks generated for today."
      });
    }

    if (items.length === 0) {
      items.push({
        type: "success",
        message: "All operations running smoothly."
      });
    }

    return items;
  }, [roomStats, cleaningStats]);

  return (
    <div className="space-y-6">
      <section className="grid gap-6 lg:grid-cols-[2fr_1fr]">
        <div className="panel p-6">
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Today</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Operational pulse</h2>
          <p className="mt-2 text-sm text-slate-500">
            Live stats for rooms, cleaning, and therapies.
          </p>
          <div className="mt-6 grid gap-4 md:grid-cols-3 animate-stagger">
            <StatCard
              label="Rooms occupied"
              value={roomsQuery.isLoading ? "..." : String(roomStats.occupied)}
              tone="tide"
            />
            <StatCard
              label="Cleaning tasks"
              value={
                cleaningSummaryQuery.isLoading
                  ? "..."
                  : cleaningStats
                  ? String(cleaningStats.pending + cleaningStats.inProgress)
                  : "—"
              }
              tone="ember"
            />
            <StatCard
              label="Therapies today"
              value={appointmentsQuery.isLoading ? "..." : String(appointmentStats.total)}
              tone="moss"
            />
          </div>
        </div>
        <div className="panel p-6">
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Focus</p>
          <h3 className="mt-2 text-xl font-semibold text-slate-900">Roles in scope</h3>
          <p className="mt-3 text-sm text-slate-500">
            Manager, cleaner, therapist. The rest of the UI stays hidden unless the role is enabled.
          </p>
          <div className="mt-4 flex flex-wrap gap-2">
            {roles.length ? (
              roles.map((role) => (
                <span key={role} className="badge bg-slate-900 text-white">
                  {role}
                </span>
              ))
            ) : (
              <span className="badge bg-amber-100 text-amber-700">Select roles to preview</span>
            )}
          </div>
        </div>
      </section>
      <section className="grid gap-6 lg:grid-cols-2">
        <div className="panel p-6">
          <h3 className="text-lg font-semibold text-slate-900">Quick actions</h3>
          <div className="mt-4 grid gap-3">
            {(isManager || isCleaner) && (
              <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-slate-800">Cleaning tasks</p>
                  <p className="text-xs text-slate-500">
                    {cleaningStats
                      ? `${cleaningStats.pending} pending, ${cleaningStats.completed} completed`
                      : "View housekeeping queue"}
                  </p>
                </div>
                <div className="flex gap-2">
                  {isManager && (
                    <button
                      onClick={() => generateMutation.mutate()}
                      disabled={generateMutation.isPending}
                      className="rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                    >
                      {generateMutation.isPending ? "..." : "Generate"}
                    </button>
                  )}
                  <Link
                    to="/housekeeping"
                    className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600"
                  >
                    View
                  </Link>
                </div>
              </div>
            )}
            {(isManager || isTherapist) && (
              <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-slate-800">Therapist schedule</p>
                  <p className="text-xs text-slate-500">
                    {appointmentStats.total} appointment{appointmentStats.total !== 1 ? "s" : ""} today
                  </p>
                </div>
                <Link
                  to="/treatments"
                  className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  View
                </Link>
              </div>
            )}
            {isManager && (
              <div className="flex items-center justify-between rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-slate-800">Room inventory</p>
                  <p className="text-xs text-slate-500">
                    {roomStats.total} rooms, {roomStats.occupied} occupied
                  </p>
                </div>
                <Link
                  to="/rooms"
                  className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  View
                </Link>
              </div>
            )}
          </div>
        </div>
        <div className="panel p-6">
          <h3 className="text-lg font-semibold text-slate-900">Alerts</h3>
          <ul className="mt-4 space-y-3 text-sm text-slate-600">
            {alerts.map((alert, index) => (
              <li key={index} className="flex items-start gap-3">
                <span
                  className={`badge ${
                    alert.type === "warning"
                      ? "bg-amber-100 text-amber-700"
                      : "bg-emerald-100 text-emerald-700"
                  }`}
                >
                  {alert.type === "warning" ? "Pending" : "On track"}
                </span>
                {alert.message}
              </li>
            ))}
          </ul>
        </div>
      </section>
    </div>
  );
}
