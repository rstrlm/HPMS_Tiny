import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getAppointments,
  updateAppointmentStatus,
  createAppointment,
  getTreatmentTypes,
  createTreatmentType,
  getTreatmentRooms,
  createTreatmentRoom,
  getTreatmentRoomAvailability
} from "../api/appointments";
import { getStaff } from "../api/staff";
import { getCustomers } from "../api/customers";
import type {
  AppointmentDto,
  CreateAppointmentRequest,
  CreateTreatmentTypeRequest,
  CreateTreatmentRoomRequest,
  TimeSlotDto
} from "../api/types";
import { getAppointmentStatusLabel } from "../lib/status";
import { hasAnyRole, useAuth } from "../state/auth";

const getStatusBadgeStyle = (status: string) => {
  switch (status) {
    case "Pending":
      return "bg-amber-100 text-amber-700";
    case "Confirmed":
      return "bg-blue-100 text-blue-700";
    case "Completed":
      return "bg-emerald-100 text-emerald-700";
    case "Cancelled":
      return "bg-rose-100 text-rose-700";
    default:
      return "bg-slate-100 text-slate-600";
  }
};

const formatDateForInput = (date: Date) => {
  return date.toISOString().split("T")[0];
};

const formatTimeFromUtc = (utcString: string) => {
  const date = new Date(utcString);
  return date.toLocaleTimeString("fi-FI", {
    timeZone: "Europe/Helsinki",
    hour: "2-digit",
    minute: "2-digit"
  });
};

export default function TherapistSchedule() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const queryClient = useQueryClient();

  const [selectedDate, setSelectedDate] = useState(() => formatDateForInput(new Date()));
  const [therapistFilter, setTherapistFilter] = useState<string | undefined>(undefined);

  // Modal state
  const [showAddModal, setShowAddModal] = useState(false);
  const [formTreatmentTypeId, setFormTreatmentTypeId] = useState("");
  const [formTreatmentRoomId, setFormTreatmentRoomId] = useState("");
  const [formTherapistId, setFormTherapistId] = useState("");
  const [formCustomerId, setFormCustomerId] = useState("");
  const [formDate, setFormDate] = useState(() => formatDateForInput(new Date()));
  const [formTimeSlot, setFormTimeSlot] = useState<TimeSlotDto | null>(null);
  const [formNotes, setFormNotes] = useState("");
  const [formSeats, setFormSeats] = useState(1);

  // Treatment type creation modal state
  const [showTypeModal, setShowTypeModal] = useState(false);
  const [typeName, setTypeName] = useState("");
  const [typeDescription, setTypeDescription] = useState("");
  const [typeDuration, setTypeDuration] = useState(60);
  const [typeBuffer, setTypeBuffer] = useState(15);
  const [typePrice, setTypePrice] = useState(0);
  const [typeRequiresTherapist, setTypeRequiresTherapist] = useState(true);

  // Treatment room creation modal state
  const [showRoomModal, setShowRoomModal] = useState(false);
  const [roomName, setRoomName] = useState("");
  const [roomDescription, setRoomDescription] = useState("");
  const [roomCapacity, setRoomCapacity] = useState(1);

  // Calculate from/to for the selected date
  const { from, to } = useMemo(() => {
    const startOfDay = new Date(`${selectedDate}T00:00:00`);
    const endOfDay = new Date(`${selectedDate}T23:59:59`);
    return {
      from: startOfDay.toISOString(),
      to: endOfDay.toISOString()
    };
  }, [selectedDate]);

  const appointmentsQuery = useQuery({
    queryKey: ["appointments", { from, to, therapistId: therapistFilter }],
    queryFn: () => getAppointments(from, to, therapistFilter)
  });

  const staffQuery = useQuery({
    queryKey: ["staff", { activeOnly: true }],
    queryFn: () => getStaff(true)
  });

  const treatmentTypesQuery = useQuery({
    queryKey: ["treatmentTypes", { activeOnly: true }],
    queryFn: () => getTreatmentTypes(true)
  });

  const treatmentRoomsQuery = useQuery({
    queryKey: ["treatmentRooms", { activeOnly: true }],
    queryFn: () => getTreatmentRooms(true)
  });

  const customersQuery = useQuery({
    queryKey: ["customers"],
    queryFn: () => getCustomers()
  });

  // Get selected treatment type to know duration
  const selectedTreatmentType = useMemo(() => {
    return treatmentTypesQuery.data?.find((t) => t.id === formTreatmentTypeId);
  }, [treatmentTypesQuery.data, formTreatmentTypeId]);

  // Get available time slots for the selected room, date, and treatment duration
  const timeSlotsQuery = useQuery({
    queryKey: ["timeSlots", formTreatmentRoomId, formDate, selectedTreatmentType?.durationMinutes, formSeats],
    queryFn: () =>
      getTreatmentRoomAvailability(
        formTreatmentRoomId,
        formDate,
        selectedTreatmentType?.durationMinutes ?? 60,
        formSeats
      ),
    enabled: !!formTreatmentRoomId && !!formDate && !!selectedTreatmentType
  });

  const confirmMutation = useMutation({
    mutationFn: (id: string) => updateAppointmentStatus(id, { status: 1 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    }
  });

  const completeMutation = useMutation({
    mutationFn: (id: string) => updateAppointmentStatus(id, { status: 2 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    }
  });

  const cancelMutation = useMutation({
    mutationFn: (id: string) => updateAppointmentStatus(id, { status: 3 }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
    }
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateAppointmentRequest) => createAppointment(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["appointments"] });
      resetForm();
      setShowAddModal(false);
    }
  });

  const createTypeMutation = useMutation({
    mutationFn: (request: CreateTreatmentTypeRequest) => createTreatmentType(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["treatmentTypes"] });
      resetTypeForm();
      setShowTypeModal(false);
    }
  });

  const createRoomMutation = useMutation({
    mutationFn: (request: CreateTreatmentRoomRequest) => createTreatmentRoom(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["treatmentRooms"] });
      resetRoomForm();
      setShowRoomModal(false);
    }
  });

  const resetForm = () => {
    setFormTreatmentTypeId("");
    setFormTreatmentRoomId("");
    setFormTherapistId("");
    setFormCustomerId("");
    setFormDate(formatDateForInput(new Date()));
    setFormTimeSlot(null);
    setFormNotes("");
    setFormSeats(1);
  };

  const resetTypeForm = () => {
    setTypeName("");
    setTypeDescription("");
    setTypeDuration(60);
    setTypeBuffer(15);
    setTypePrice(0);
    setTypeRequiresTherapist(true);
  };

  const resetRoomForm = () => {
    setRoomName("");
    setRoomDescription("");
    setRoomCapacity(1);
  };

  const handleCreateTreatmentType = () => {
    if (!typeName || typeDuration <= 0) return;

    createTypeMutation.mutate({
      name: typeName,
      description: typeDescription || undefined,
      durationMinutes: typeDuration,
      bufferMinutes: typeBuffer,
      basePrice: typePrice,
      requiresTherapist: typeRequiresTherapist
    });
  };

  const handleCreateTreatmentRoom = () => {
    if (!roomName || roomCapacity <= 0) return;

    createRoomMutation.mutate({
      name: roomName,
      description: roomDescription || undefined,
      capacity: roomCapacity
    });
  };

  const handleCreateAppointment = () => {
    if (!formTreatmentTypeId || !formTreatmentRoomId || !formTimeSlot) return;

    const request: CreateAppointmentRequest = {
      treatmentTypeId: formTreatmentTypeId,
      treatmentRoomId: formTreatmentRoomId,
      startAtUtc: formTimeSlot.startUtc,
      durationMinutes: selectedTreatmentType?.durationMinutes ?? 60,
      seatsUsed: formSeats,
      therapistId: formTherapistId || undefined,
      customerId: formCustomerId || undefined,
      notes: formNotes || undefined
    };

    createMutation.mutate(request);
  };

  const appointments = appointmentsQuery.data ?? [];
  const therapists = staffQuery.data?.filter((s) => s.skills?.toLowerCase().includes("therapist")) ?? [];

  // Sort by start time
  const sortedAppointments = useMemo(() => {
    return [...appointments].sort(
      (a, b) => new Date(a.startAtUtc).getTime() - new Date(b.startAtUtc).getTime()
    );
  }, [appointments]);

  const metrics = useMemo(() => {
    const statusCounts = appointments.reduce<Record<string, number>>((acc, appt) => {
      const label = getAppointmentStatusLabel(appt.status);
      acc[label] = (acc[label] ?? 0) + 1;
      return acc;
    }, {});

    return [
      { label: "Total", value: String(appointments.length) },
      { label: "Pending", value: String(statusCounts["Pending"] ?? 0) },
      { label: "Confirmed", value: String(statusCounts["Confirmed"] ?? 0) },
      { label: "Completed", value: String(statusCounts["Completed"] ?? 0) }
    ];
  }, [appointments]);

  const handleAction = (appointment: AppointmentDto, action: "confirm" | "complete" | "cancel") => {
    switch (action) {
      case "confirm":
        confirmMutation.mutate(appointment.id);
        break;
      case "complete":
        completeMutation.mutate(appointment.id);
        break;
      case "cancel":
        cancelMutation.mutate(appointment.id);
        break;
    }
  };

  const isActionPending =
    confirmMutation.isPending || completeMutation.isPending || cancelMutation.isPending || createMutation.isPending;

  const treatmentTypes = treatmentTypesQuery.data ?? [];
  const treatmentRooms = treatmentRoomsQuery.data ?? [];
  const customers = customersQuery.data ?? [];
  const timeSlots = timeSlotsQuery.data ?? [];

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Therapies</p>
          <h2 className="text-2xl font-semibold text-slate-900">Therapist schedule</h2>
          <p className="mt-1 text-sm text-slate-500">Booked appointments for the day.</p>
        </div>
        {isManager && (
          <div className="flex gap-2">
            <button
              onClick={() => setShowTypeModal(true)}
              className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
            >
              + Treatment Type
            </button>
            <button
              onClick={() => setShowRoomModal(true)}
              className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
            >
              + Treatment Room
            </button>
            <button
              onClick={() => setShowAddModal(true)}
              className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
            >
              Add appointment
            </button>
          </div>
        )}
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
          <h3 className="text-lg font-semibold text-slate-900">Timeline</h3>
          <div className="flex items-center gap-2">
            <input
              type="date"
              value={selectedDate}
              onChange={(e) => setSelectedDate(e.target.value)}
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
            />
            {isManager && therapists.length > 0 && (
              <select
                value={therapistFilter ?? ""}
                onChange={(e) => setTherapistFilter(e.target.value || undefined)}
                className="rounded-full border border-slate-200 px-4 py-2 text-sm"
              >
                <option value="">All therapists</option>
                {therapists.map((t) => (
                  <option key={t.id} value={t.id}>
                    {t.displayName}
                  </option>
                ))}
              </select>
            )}
          </div>
        </div>
        <div className="mt-4 grid gap-3">
          {appointmentsQuery.isLoading && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
              Loading appointments...
            </div>
          )}
          {appointmentsQuery.isError && (
            <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-6 text-center text-sm text-rose-600">
              Failed to load appointments. Check your token or API.
            </div>
          )}
          {!appointmentsQuery.isLoading && !appointmentsQuery.isError && sortedAppointments.length === 0 && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-center text-sm text-slate-500">
              No appointments for this date.
            </div>
          )}
          {sortedAppointments.map((appointment) => {
            const statusLabel = getAppointmentStatusLabel(appointment.status);

            return (
              <div
                key={appointment.id}
                className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4"
              >
                <div>
                  <p className="text-xs uppercase tracking-[0.2em] text-slate-400">
                    {formatTimeFromUtc(appointment.startAtUtc)} -{" "}
                    {formatTimeFromUtc(appointment.endAtUtc)}
                  </p>
                  <p className="text-lg font-semibold text-slate-900">
                    {appointment.treatmentTypeName ?? "Treatment"}
                  </p>
                  <p className="text-sm text-slate-500">
                    Room: {appointment.treatmentRoomName ?? "—"}
                    {appointment.therapistName && ` | ${appointment.therapistName}`}
                  </p>
                  {appointment.customerName && (
                    <p className="text-sm text-slate-500">Guest: {appointment.customerName}</p>
                  )}
                </div>
                <div className="text-right">
                  <span className={`badge ${getStatusBadgeStyle(statusLabel)}`}>{statusLabel}</span>
                  {appointment.seatsUsed > 1 && (
                    <p className="mt-1 text-xs text-slate-500">{appointment.seatsUsed} seats</p>
                  )}
                </div>
                <div className="flex gap-2">
                  {statusLabel === "Pending" && (
                    <>
                      <button
                        onClick={() => handleAction(appointment, "confirm")}
                        disabled={isActionPending}
                        className="rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                      >
                        Confirm
                      </button>
                      <button
                        onClick={() => handleAction(appointment, "cancel")}
                        disabled={isActionPending}
                        className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 disabled:opacity-50"
                      >
                        Cancel
                      </button>
                    </>
                  )}
                  {statusLabel === "Confirmed" && (
                    <button
                      onClick={() => handleAction(appointment, "complete")}
                      disabled={isActionPending}
                      className="rounded-full bg-emerald-600 px-4 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                    >
                      Complete
                    </button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      </section>

      {/* Add Appointment Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">New</p>
              <h3 className="text-xl font-semibold text-slate-900">Add appointment</h3>
            </div>

            {treatmentTypes.length === 0 && (
              <div className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
                <p className="text-sm text-amber-700">
                  No treatment types available. Please create treatment types first.
                </p>
              </div>
            )}

            {treatmentRooms.length === 0 && (
              <div className="mb-4 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
                <p className="text-sm text-amber-700">
                  No treatment rooms available. Please create treatment rooms first.
                </p>
              </div>
            )}

            <div className="space-y-4">
              {/* Treatment Type */}
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Treatment Type *
                </label>
                <select
                  value={formTreatmentTypeId}
                  onChange={(e) => {
                    setFormTreatmentTypeId(e.target.value);
                    setFormTimeSlot(null);
                  }}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value="">Select treatment type</option>
                  {treatmentTypes.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.name} ({t.durationMinutes} min) - €{t.basePrice.toFixed(2)}
                    </option>
                  ))}
                </select>
              </div>

              {/* Treatment Room */}
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Treatment Room *
                </label>
                <select
                  value={formTreatmentRoomId}
                  onChange={(e) => {
                    setFormTreatmentRoomId(e.target.value);
                    setFormTimeSlot(null);
                  }}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value="">Select room</option>
                  {treatmentRooms.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.name} (capacity: {r.capacity})
                    </option>
                  ))}
                </select>
              </div>

              {/* Date and Seats */}
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                    Date *
                  </label>
                  <input
                    type="date"
                    value={formDate}
                    onChange={(e) => {
                      setFormDate(e.target.value);
                      setFormTimeSlot(null);
                    }}
                    className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                    Seats
                  </label>
                  <input
                    type="number"
                    min={1}
                    value={formSeats}
                    onChange={(e) => {
                      setFormSeats(parseInt(e.target.value) || 1);
                      setFormTimeSlot(null);
                    }}
                    className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  />
                </div>
              </div>

              {/* Time Slot */}
              {formTreatmentTypeId && formTreatmentRoomId && formDate && (
                <div>
                  <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                    Time Slot *
                  </label>
                  {timeSlotsQuery.isLoading && (
                    <p className="text-sm text-slate-500">Loading available slots...</p>
                  )}
                  {timeSlotsQuery.isError && (
                    <p className="text-sm text-rose-600">Failed to load time slots</p>
                  )}
                  {!timeSlotsQuery.isLoading && timeSlots.length === 0 && (
                    <p className="text-sm text-amber-600">No available slots for this date</p>
                  )}
                  {timeSlots.length > 0 && (
                    <div className="grid max-h-32 grid-cols-4 gap-2 overflow-y-auto">
                      {timeSlots.map((slot) => (
                        <button
                          key={slot.startUtc}
                          type="button"
                          onClick={() => setFormTimeSlot(slot)}
                          className={`rounded-lg border px-2 py-1 text-xs ${
                            formTimeSlot?.startUtc === slot.startUtc
                              ? "border-slate-900 bg-slate-900 text-white"
                              : "border-slate-200 text-slate-600 hover:bg-slate-50"
                          }`}
                        >
                          {formatTimeFromUtc(slot.startUtc)}
                        </button>
                      ))}
                    </div>
                  )}
                </div>
              )}

              {/* Therapist */}
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Therapist
                </label>
                <select
                  value={formTherapistId}
                  onChange={(e) => setFormTherapistId(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value="">No therapist assigned</option>
                  {therapists.map((t) => (
                    <option key={t.id} value={t.id}>
                      {t.displayName}
                    </option>
                  ))}
                </select>
              </div>

              {/* Customer */}
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Customer
                </label>
                <select
                  value={formCustomerId}
                  onChange={(e) => setFormCustomerId(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                >
                  <option value="">Walk-in / No customer</option>
                  {customers.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name} {c.phone && `(${c.phone})`}
                    </option>
                  ))}
                </select>
              </div>

              {/* Notes */}
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
                <p className="text-sm text-rose-700">
                  Failed to create appointment. Please try again.
                </p>
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
                onClick={handleCreateAppointment}
                disabled={
                  !formTreatmentTypeId ||
                  !formTreatmentRoomId ||
                  !formTimeSlot ||
                  createMutation.isPending
                }
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {createMutation.isPending ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Treatment Type Creation Modal */}
      {showTypeModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">New</p>
              <h3 className="text-xl font-semibold text-slate-900">Add treatment type</h3>
            </div>

            <div className="space-y-4">
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Name *
                </label>
                <input
                  type="text"
                  value={typeName}
                  onChange={(e) => setTypeName(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  placeholder="e.g., Hot Stone Massage"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Description
                </label>
                <textarea
                  value={typeDescription}
                  onChange={(e) => setTypeDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  placeholder="Optional description..."
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                    Duration (min) *
                  </label>
                  <input
                    type="number"
                    min={15}
                    value={typeDuration}
                    onChange={(e) => setTypeDuration(parseInt(e.target.value) || 60)}
                    className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  />
                </div>
                <div>
                  <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                    Buffer (min)
                  </label>
                  <input
                    type="number"
                    min={0}
                    value={typeBuffer}
                    onChange={(e) => setTypeBuffer(parseInt(e.target.value) || 0)}
                    className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  />
                </div>
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Base Price (€)
                </label>
                <input
                  type="number"
                  min={0}
                  step={0.01}
                  value={typePrice}
                  onChange={(e) => setTypePrice(parseFloat(e.target.value) || 0)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                />
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="requiresTherapist"
                  checked={typeRequiresTherapist}
                  onChange={(e) => setTypeRequiresTherapist(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300"
                />
                <label htmlFor="requiresTherapist" className="text-sm text-slate-600">
                  Requires therapist
                </label>
              </div>
            </div>

            {createTypeMutation.isError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to create treatment type.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => {
                  resetTypeForm();
                  setShowTypeModal(false);
                }}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleCreateTreatmentType}
                disabled={!typeName || typeDuration <= 0 || createTypeMutation.isPending}
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {createTypeMutation.isPending ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Treatment Room Creation Modal */}
      {showRoomModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">New</p>
              <h3 className="text-xl font-semibold text-slate-900">Add treatment room</h3>
            </div>

            <div className="space-y-4">
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Name *
                </label>
                <input
                  type="text"
                  value={roomName}
                  onChange={(e) => setRoomName(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  placeholder="e.g., Spa Room 1"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Description
                </label>
                <textarea
                  value={roomDescription}
                  onChange={(e) => setRoomDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                  placeholder="Optional description..."
                />
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Capacity *
                </label>
                <input
                  type="number"
                  min={1}
                  value={roomCapacity}
                  onChange={(e) => setRoomCapacity(parseInt(e.target.value) || 1)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                />
                <p className="mt-1 text-xs text-slate-400">
                  How many concurrent appointments can this room handle
                </p>
              </div>
            </div>

            {createRoomMutation.isError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to create treatment room.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => {
                  resetRoomForm();
                  setShowRoomModal(false);
                }}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleCreateTreatmentRoom}
                disabled={!roomName || roomCapacity <= 0 || createRoomMutation.isPending}
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {createRoomMutation.isPending ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
