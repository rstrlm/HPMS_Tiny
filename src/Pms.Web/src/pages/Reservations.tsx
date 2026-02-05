import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import StatusPill from "../components/StatusPill";
import {
  getReservations,
  createReservation,
  changeReservationStatus,
  getRoomAvailability
} from "../api/reservations";
import { getCustomers } from "../api/customers";
import { getTreatmentTypes, getTreatmentRooms } from "../api/appointments";
import { getStaff } from "../api/staff";
import type {
  ReservationDto,
  CreateReservationRequest,
  CustomerDto,
  RoomAvailabilityInfo,
  TreatmentTypeDto,
  TreatmentRoomDto,
  StaffProfileDto,
  CreateReservationAppointmentRequest
} from "../api/types";

const RESERVATION_STATUS_LABELS: Record<string, string> = {
  "0": "Pending",
  "1": "Confirmed",
  "2": "CheckedIn",
  "3": "CheckedOut",
  "4": "Cancelled"
};

const RESERVATION_STATUS_OPTIONS = [
  { value: 0, label: "Pending" },
  { value: 1, label: "Confirmed" },
  { value: 2, label: "Checked In" },
  { value: 3, label: "Checked Out" },
  { value: 4, label: "Cancelled" }
];

const getStatusLabel = (status: number | string) => {
  if (typeof status === "number") return RESERVATION_STATUS_LABELS[String(status)] ?? "Unknown";
  return RESERVATION_STATUS_LABELS[status] ?? status;
};

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString();
};

type CustomerMode = "select" | "create";

type NewCustomerData = {
  name: string;
  phone: string;
  email: string;
  address: string;
  notes: string;
};

type TreatmentSelection = {
  treatmentTypeId: string;
  treatmentRoomId: string;
  therapistStaffId: string;
  startAtUtc: string;
  notes: string;
};

type ReservationFormData = {
  customerMode: CustomerMode;
  customerId: string;
  newCustomer: NewCustomerData;
  checkInDate: string;
  checkOutDate: string;
  numberOfGuests: number;
  notes: string;
  selectedRoomIds: string[];
  treatments: TreatmentSelection[];
  showTreatments: boolean;
};

const emptyNewCustomer: NewCustomerData = {
  name: "",
  phone: "",
  email: "",
  address: "",
  notes: ""
};

const emptyTreatment: TreatmentSelection = {
  treatmentTypeId: "",
  treatmentRoomId: "",
  therapistStaffId: "",
  startAtUtc: "",
  notes: ""
};

const emptyFormData: ReservationFormData = {
  customerMode: "select",
  customerId: "",
  newCustomer: emptyNewCustomer,
  checkInDate: "",
  checkOutDate: "",
  numberOfGuests: 1,
  notes: "",
  selectedRoomIds: [],
  treatments: [],
  showTreatments: false
};

export default function Reservations() {
  const queryClient = useQueryClient();

  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [dateFrom, setDateFrom] = useState("");
  const [dateTo, setDateTo] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [formData, setFormData] = useState<ReservationFormData>(emptyFormData);
  const [statusModal, setStatusModal] = useState<{ reservation: ReservationDto; newStatus: number } | null>(null);

  // Queries
  const reservationsQuery = useQuery({
    queryKey: ["reservations", { from: dateFrom, to: dateTo, status: statusFilter }],
    queryFn: () => getReservations(dateFrom || undefined, dateTo || undefined, statusFilter)
  });

  const customersQuery = useQuery({
    queryKey: ["customers"],
    queryFn: () => getCustomers()
  });

  const availabilityQuery = useQuery({
    queryKey: ["roomAvailability", { from: formData.checkInDate, to: formData.checkOutDate }],
    queryFn: () => getRoomAvailability(formData.checkInDate, formData.checkOutDate),
    enabled: modalOpen && !!formData.checkInDate && !!formData.checkOutDate
  });

  const treatmentTypesQuery = useQuery({
    queryKey: ["treatmentTypes"],
    queryFn: () => getTreatmentTypes(),
    enabled: modalOpen && formData.showTreatments
  });

  const treatmentRoomsQuery = useQuery({
    queryKey: ["treatmentRooms"],
    queryFn: () => getTreatmentRooms(),
    enabled: modalOpen && formData.showTreatments
  });

  const therapistsQuery = useQuery({
    queryKey: ["staff", { activeOnly: true }],
    queryFn: () => getStaff(true),
    enabled: modalOpen && formData.showTreatments
  });

  // Mutations
  const createMutation = useMutation({
    mutationFn: (request: CreateReservationRequest) => createReservation(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations"] });
      queryClient.invalidateQueries({ queryKey: ["customers"] });
      closeModal();
    }
  });

  const statusMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) =>
      changeReservationStatus(id, { status }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["reservations"] });
      setStatusModal(null);
    }
  });

  const reservations = reservationsQuery.data ?? [];
  const customers = customersQuery.data ?? [];
  const availableRooms = availabilityQuery.data ?? [];
  const treatmentTypes = treatmentTypesQuery.data ?? [];
  const treatmentRooms = treatmentRoomsQuery.data ?? [];
  const therapists = (therapistsQuery.data ?? []).filter(
    (s: StaffProfileDto) => s.skills?.toLowerCase().includes("therapist")
  );

  // Stats
  const stats = useMemo(() => {
    const statusCounts = reservations.reduce<Record<string, number>>((acc, res) => {
      const label = getStatusLabel(res.status);
      acc[label] = (acc[label] ?? 0) + 1;
      return acc;
    }, {});

    return [
      { label: "Total", value: String(reservations.length) },
      { label: "Pending", value: String(statusCounts["Pending"] ?? 0) },
      { label: "Checked In", value: String(statusCounts["CheckedIn"] ?? 0) }
    ];
  }, [reservations]);

  const openCreateModal = () => {
    const today = new Date().toISOString().split("T")[0];
    const tomorrow = new Date(Date.now() + 86400000).toISOString().split("T")[0];
    setFormData({
      ...emptyFormData,
      checkInDate: today,
      checkOutDate: tomorrow
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setFormData(emptyFormData);
  };

  const toggleRoomSelection = (roomId: string) => {
    setFormData((prev) => ({
      ...prev,
      selectedRoomIds: prev.selectedRoomIds.includes(roomId)
        ? prev.selectedRoomIds.filter((id) => id !== roomId)
        : [...prev.selectedRoomIds, roomId]
    }));
  };

  const addTreatment = () => {
    setFormData((prev) => ({
      ...prev,
      treatments: [...prev.treatments, { ...emptyTreatment }]
    }));
  };

  const removeTreatment = (index: number) => {
    setFormData((prev) => ({
      ...prev,
      treatments: prev.treatments.filter((_, i) => i !== index)
    }));
  };

  const updateTreatment = (index: number, field: keyof TreatmentSelection, value: string) => {
    setFormData((prev) => ({
      ...prev,
      treatments: prev.treatments.map((t, i) =>
        i === index ? { ...t, [field]: value } : t
      )
    }));
  };

  const isCustomerValid =
    formData.customerMode === "select"
      ? !!formData.customerId
      : !!formData.newCustomer.name;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!isCustomerValid || formData.selectedRoomIds.length === 0) return;

    const appointments: CreateReservationAppointmentRequest[] = formData.treatments
      .filter((t) => t.treatmentTypeId && t.treatmentRoomId && t.startAtUtc)
      .map((t) => ({
        treatmentTypeId: t.treatmentTypeId,
        treatmentRoomId: t.treatmentRoomId,
        therapistStaffId: t.therapistStaffId || undefined,
        startAtUtc: t.startAtUtc,
        notes: t.notes || undefined
      }));

    const request: CreateReservationRequest = {
      checkInDate: formData.checkInDate,
      checkOutDate: formData.checkOutDate,
      numberOfGuests: formData.numberOfGuests,
      notes: formData.notes || undefined,
      roomAssignments: formData.selectedRoomIds.map((roomId) => ({
        roomId,
        fromDate: formData.checkInDate,
        toDate: formData.checkOutDate
      })),
      appointments: appointments.length > 0 ? appointments : undefined
    };

    if (formData.customerMode === "select") {
      request.customerId = formData.customerId;
    } else {
      request.newCustomer = {
        name: formData.newCustomer.name,
        phone: formData.newCustomer.phone || undefined,
        email: formData.newCustomer.email || undefined,
        address: formData.newCustomer.address || undefined,
        notes: formData.newCustomer.notes || undefined
      };
    }

    createMutation.mutate(request);
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Bookings</p>
          <h2 className="text-2xl font-semibold text-slate-900">Reservations</h2>
          <p className="mt-1 text-sm text-slate-500">Manage guest reservations and room assignments.</p>
        </div>
        <button
          onClick={openCreateModal}
          className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
        >
          New reservation
        </button>
      </header>

      <section className="grid gap-4 md:grid-cols-3">
        {stats.map((stat) => (
          <div key={stat.label} className="panel px-4 py-5">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-400">{stat.label}</p>
            <p className="mt-3 text-3xl font-semibold text-slate-900">{stat.value}</p>
          </div>
        ))}
      </section>

      <section className="panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-900">Reservation list</h3>
          <div className="flex flex-wrap items-center gap-3">
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="rounded-xl border border-slate-200 px-3 py-2 text-sm"
              placeholder="From"
            />
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="rounded-xl border border-slate-200 px-3 py-2 text-sm"
              placeholder="To"
            />
            <select
              value={statusFilter ?? ""}
              onChange={(e) => setStatusFilter(e.target.value ? Number(e.target.value) : undefined)}
              className="rounded-xl border border-slate-200 px-3 py-2 text-sm"
            >
              <option value="">All statuses</option>
              {RESERVATION_STATUS_OPTIONS.map((opt) => (
                <option key={opt.value} value={opt.value}>
                  {opt.label}
                </option>
              ))}
            </select>
          </div>
        </div>
        <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-[0.2em] text-slate-400">
              <tr>
                <th className="px-4 py-3">Customer</th>
                <th className="px-4 py-3">Check-in</th>
                <th className="px-4 py-3">Check-out</th>
                <th className="px-4 py-3">Rooms</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody>
              {reservationsQuery.isLoading && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-slate-500">
                    Loading reservations...
                  </td>
                </tr>
              )}
              {reservationsQuery.isError && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-rose-500">
                    Failed to load reservations.
                  </td>
                </tr>
              )}
              {!reservationsQuery.isLoading && !reservationsQuery.isError && reservations.length === 0 && (
                <tr>
                  <td colSpan={6} className="px-4 py-6 text-center text-sm text-slate-500">
                    No reservations found.
                  </td>
                </tr>
              )}
              {reservations.map((reservation: ReservationDto) => (
                <tr key={reservation.id} className="border-t border-slate-100">
                  <td className="px-4 py-3 font-semibold text-slate-900">
                    {reservation.customerName ?? "—"}
                  </td>
                  <td className="px-4 py-3 text-slate-600">{formatDate(reservation.checkInDate)}</td>
                  <td className="px-4 py-3 text-slate-600">{formatDate(reservation.checkOutDate)}</td>
                  <td className="px-4 py-3 text-slate-600">
                    {reservation.roomAssignments.length > 0
                      ? reservation.roomAssignments.map((ra) => ra.roomNumber).join(", ")
                      : "—"}
                  </td>
                  <td className="px-4 py-3">
                    <StatusPill status={getStatusLabel(reservation.status)} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    <select
                      value=""
                      onChange={(e) => {
                        if (e.target.value) {
                          setStatusModal({
                            reservation,
                            newStatus: Number(e.target.value)
                          });
                        }
                      }}
                      className="rounded-full border border-slate-200 px-3 py-1 text-xs font-semibold uppercase text-slate-600"
                    >
                      <option value="">Change status</option>
                      {RESERVATION_STATUS_OPTIONS.filter(
                        (opt) => opt.value !== reservation.status
                      ).map((opt) => (
                        <option key={opt.value} value={opt.value}>
                          {opt.label}
                        </option>
                      ))}
                    </select>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* Create Reservation Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="max-h-[90vh] w-full max-w-3xl overflow-y-auto rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">New Reservation</h3>
            <form onSubmit={handleSubmit} className="mt-4 space-y-6">
              {/* Customer Section */}
              <div className="rounded-xl border border-slate-200 p-4">
                <div className="flex items-center justify-between">
                  <h4 className="text-sm font-semibold text-slate-900">Customer</h4>
                  <div className="flex rounded-lg border border-slate-200 p-0.5">
                    <button
                      type="button"
                      onClick={() => setFormData({ ...formData, customerMode: "select" })}
                      className={`rounded-md px-3 py-1 text-xs font-semibold transition ${
                        formData.customerMode === "select"
                          ? "bg-slate-900 text-white"
                          : "text-slate-600 hover:bg-slate-50"
                      }`}
                    >
                      Select Existing
                    </button>
                    <button
                      type="button"
                      onClick={() => setFormData({ ...formData, customerMode: "create" })}
                      className={`rounded-md px-3 py-1 text-xs font-semibold transition ${
                        formData.customerMode === "create"
                          ? "bg-slate-900 text-white"
                          : "text-slate-600 hover:bg-slate-50"
                      }`}
                    >
                      Create New
                    </button>
                  </div>
                </div>

                {formData.customerMode === "select" ? (
                  <div className="mt-4">
                    <select
                      value={formData.customerId}
                      onChange={(e) => setFormData({ ...formData, customerId: e.target.value })}
                      className="w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                      required={formData.customerMode === "select"}
                    >
                      <option value="">Select customer</option>
                      {customers.map((c: CustomerDto) => (
                        <option key={c.id} value={c.id}>
                          {c.name} {c.email ? `(${c.email})` : ""}
                        </option>
                      ))}
                    </select>
                  </div>
                ) : (
                  <div className="mt-4 grid gap-4 md:grid-cols-2">
                    <div>
                      <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                        Name *
                      </label>
                      <input
                        type="text"
                        value={formData.newCustomer.name}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            newCustomer: { ...formData.newCustomer, name: e.target.value }
                          })
                        }
                        className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                        required={formData.customerMode === "create"}
                        placeholder="John Smith"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                        Email
                      </label>
                      <input
                        type="email"
                        value={formData.newCustomer.email}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            newCustomer: { ...formData.newCustomer, email: e.target.value }
                          })
                        }
                        className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                        placeholder="john@example.com"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                        Phone
                      </label>
                      <input
                        type="tel"
                        value={formData.newCustomer.phone}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            newCustomer: { ...formData.newCustomer, phone: e.target.value }
                          })
                        }
                        className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                        placeholder="+358 40 123 4567"
                      />
                    </div>
                    <div>
                      <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                        Address
                      </label>
                      <input
                        type="text"
                        value={formData.newCustomer.address}
                        onChange={(e) =>
                          setFormData({
                            ...formData,
                            newCustomer: { ...formData.newCustomer, address: e.target.value }
                          })
                        }
                        className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                        placeholder="123 Main St, Helsinki"
                      />
                    </div>
                  </div>
                )}
              </div>

              {/* Dates and Guests */}
              <div className="grid gap-4 md:grid-cols-3">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Check-in *
                  </label>
                  <input
                    type="date"
                    value={formData.checkInDate}
                    onChange={(e) => setFormData({ ...formData, checkInDate: e.target.value })}
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Check-out *
                  </label>
                  <input
                    type="date"
                    value={formData.checkOutDate}
                    onChange={(e) => setFormData({ ...formData, checkOutDate: e.target.value })}
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Guests
                  </label>
                  <input
                    type="number"
                    min="1"
                    value={formData.numberOfGuests}
                    onChange={(e) =>
                      setFormData({ ...formData, numberOfGuests: Number(e.target.value) })
                    }
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  />
                </div>
              </div>

              {/* Notes */}
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Notes
                </label>
                <textarea
                  value={formData.notes}
                  onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  rows={2}
                />
              </div>

              {/* Room Selection */}
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Select Rooms *
                </label>
                {availabilityQuery.isLoading && (
                  <p className="mt-2 text-sm text-slate-500">Checking availability...</p>
                )}
                {!formData.checkInDate || !formData.checkOutDate ? (
                  <p className="mt-2 text-sm text-slate-500">
                    Select dates to see available rooms
                  </p>
                ) : availableRooms.length === 0 && !availabilityQuery.isLoading ? (
                  <p className="mt-2 text-sm text-amber-600">No rooms available for selected dates</p>
                ) : (
                  <div className="mt-2 grid gap-2 md:grid-cols-2">
                    {availableRooms.map((room: RoomAvailabilityInfo) => (
                      <label
                        key={room.roomId}
                        className={`flex cursor-pointer items-center gap-3 rounded-xl border px-4 py-3 transition ${
                          room.isAvailable
                            ? formData.selectedRoomIds.includes(room.roomId)
                              ? "border-slate-900 bg-slate-50"
                              : "border-slate-200 hover:border-slate-300"
                            : "cursor-not-allowed border-slate-100 bg-slate-50 opacity-50"
                        }`}
                      >
                        <input
                          type="checkbox"
                          checked={formData.selectedRoomIds.includes(room.roomId)}
                          onChange={() => toggleRoomSelection(room.roomId)}
                          disabled={!room.isAvailable}
                          className="accent-slate-900"
                        />
                        <div>
                          <p className="font-semibold text-slate-900">{room.roomNumber}</p>
                          <p className="text-xs text-slate-500">{room.roomTypeName}</p>
                        </div>
                        {!room.isAvailable && (
                          <span className="ml-auto text-xs text-rose-500">Unavailable</span>
                        )}
                      </label>
                    ))}
                  </div>
                )}
              </div>

              {/* Treatments Section */}
              <div className="rounded-xl border border-slate-200 p-4">
                <div className="flex items-center justify-between">
                  <h4 className="text-sm font-semibold text-slate-900">Spa Treatments (Optional)</h4>
                  <button
                    type="button"
                    onClick={() => setFormData({ ...formData, showTreatments: !formData.showTreatments })}
                    className="text-xs font-semibold text-indigo-600 hover:text-indigo-700"
                  >
                    {formData.showTreatments ? "Hide" : "Add Treatments"}
                  </button>
                </div>

                {formData.showTreatments && (
                  <div className="mt-4 space-y-4">
                    {formData.treatments.length === 0 && (
                      <p className="text-sm text-slate-500">No treatments added yet.</p>
                    )}

                    {formData.treatments.map((treatment, index) => (
                      <div key={index} className="rounded-lg border border-slate-100 bg-slate-50 p-4">
                        <div className="flex items-center justify-between mb-3">
                          <span className="text-xs font-semibold uppercase text-slate-400">
                            Treatment {index + 1}
                          </span>
                          <button
                            type="button"
                            onClick={() => removeTreatment(index)}
                            className="text-xs text-rose-600 hover:text-rose-700"
                          >
                            Remove
                          </button>
                        </div>
                        <div className="grid gap-3 md:grid-cols-2">
                          <div>
                            <label className="block text-xs text-slate-500">Treatment Type</label>
                            <select
                              value={treatment.treatmentTypeId}
                              onChange={(e) => updateTreatment(index, "treatmentTypeId", e.target.value)}
                              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                            >
                              <option value="">Select treatment</option>
                              {treatmentTypes
                                .filter((t: TreatmentTypeDto) => t.isActive)
                                .map((t: TreatmentTypeDto) => (
                                  <option key={t.id} value={t.id}>
                                    {t.name} ({t.durationMinutes} min)
                                  </option>
                                ))}
                            </select>
                          </div>
                          <div>
                            <label className="block text-xs text-slate-500">Treatment Room</label>
                            <select
                              value={treatment.treatmentRoomId}
                              onChange={(e) => updateTreatment(index, "treatmentRoomId", e.target.value)}
                              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                            >
                              <option value="">Select room</option>
                              {treatmentRooms
                                .filter((r: TreatmentRoomDto) => r.isActive)
                                .map((r: TreatmentRoomDto) => (
                                  <option key={r.id} value={r.id}>
                                    {r.name}
                                  </option>
                                ))}
                            </select>
                          </div>
                          <div>
                            <label className="block text-xs text-slate-500">Therapist (optional)</label>
                            <select
                              value={treatment.therapistStaffId}
                              onChange={(e) => updateTreatment(index, "therapistStaffId", e.target.value)}
                              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                            >
                              <option value="">Any available</option>
                              {therapists.map((s: StaffProfileDto) => (
                                <option key={s.id} value={s.id}>
                                  {s.displayName}
                                </option>
                              ))}
                            </select>
                          </div>
                          <div>
                            <label className="block text-xs text-slate-500">Date & Time</label>
                            <input
                              type="datetime-local"
                              value={treatment.startAtUtc ? treatment.startAtUtc.slice(0, 16) : ""}
                              onChange={(e) =>
                                updateTreatment(index, "startAtUtc", e.target.value ? new Date(e.target.value).toISOString() : "")
                              }
                              className="mt-1 w-full rounded-lg border border-slate-200 px-3 py-2 text-sm"
                            />
                          </div>
                        </div>
                      </div>
                    ))}

                    <button
                      type="button"
                      onClick={addTreatment}
                      className="w-full rounded-lg border-2 border-dashed border-slate-200 px-4 py-3 text-sm font-semibold text-slate-600 hover:border-slate-300 hover:bg-slate-50"
                    >
                      + Add Treatment
                    </button>
                  </div>
                )}
              </div>

              {/* Form Actions */}
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={
                    createMutation.isPending ||
                    !isCustomerValid ||
                    formData.selectedRoomIds.length === 0
                  }
                  className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {createMutation.isPending ? "Creating..." : "Create Reservation"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Status Change Confirmation Modal */}
      {statusModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Change Status</h3>
            <p className="mt-2 text-sm text-slate-600">
              Change reservation for{" "}
              <span className="font-semibold">{statusModal.reservation.customerName}</span> to{" "}
              <span className="font-semibold">
                {RESERVATION_STATUS_OPTIONS.find((o) => o.value === statusModal.newStatus)?.label}
              </span>
              ?
            </p>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setStatusModal(null)}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={() =>
                  statusMutation.mutate({
                    id: statusModal.reservation.id,
                    status: statusModal.newStatus
                  })
                }
                disabled={statusMutation.isPending}
                className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
              >
                {statusMutation.isPending ? "Updating..." : "Confirm"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
