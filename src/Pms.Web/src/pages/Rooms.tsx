import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import StatusPill from "../components/StatusPill";
import {
  getRooms,
  getRoomTypes,
  createRoom,
  updateRoom,
  deleteRoom,
  createRoomType
} from "../api/rooms";
import type {
  RoomDto,
  RoomTypeDto,
  CreateRoomRequest,
  UpdateRoomRequest,
  CreateRoomTypeRequest
} from "../api/types";
import { getRoomStatusLabel, ROOM_STATUS_OPTIONS } from "../lib/room";
import { hasAnyRole, useAuth } from "../state/auth";

type RoomFormData = {
  roomNumber: string;
  roomTypeId: string;
  isActive: boolean;
  currentStatus: number;
};

const emptyFormData: RoomFormData = {
  roomNumber: "",
  roomTypeId: "",
  isActive: true,
  currentStatus: 0
};

type RoomTypeFormData = {
  name: string;
  description: string;
  capacity: number;
  basePrice: number;
};

const emptyRoomTypeForm: RoomTypeFormData = {
  name: "",
  description: "",
  capacity: 2,
  basePrice: 100
};

export default function Rooms() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [activeOnly, setActiveOnly] = useState(true);
  const [modalOpen, setModalOpen] = useState(false);
  const [editingRoom, setEditingRoom] = useState<RoomDto | null>(null);
  const [formData, setFormData] = useState<RoomFormData>(emptyFormData);
  const [deleteConfirm, setDeleteConfirm] = useState<RoomDto | null>(null);
  const [roomTypeModalOpen, setRoomTypeModalOpen] = useState(false);
  const [roomTypeForm, setRoomTypeForm] = useState<RoomTypeFormData>(emptyRoomTypeForm);

  const roomsQuery = useQuery({
    queryKey: ["rooms", { activeOnly }],
    queryFn: () => getRooms(activeOnly)
  });

  const roomTypesQuery = useQuery({
    queryKey: ["roomTypes"],
    queryFn: getRoomTypes
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateRoomRequest) => createRoom(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rooms"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateRoomRequest }) =>
      updateRoom(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rooms"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteRoom(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["rooms"] });
      setDeleteConfirm(null);
    }
  });

  const createRoomTypeMutation = useMutation({
    mutationFn: (request: CreateRoomTypeRequest) => createRoomType(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["roomTypes"] });
      setRoomTypeModalOpen(false);
      setRoomTypeForm(emptyRoomTypeForm);
    }
  });

  const rooms = roomsQuery.data ?? [];
  const roomTypes = roomTypesQuery.data ?? [];

  const filteredRooms = useMemo(() => {
    if (!search) return rooms;
    const term = search.toLowerCase();
    return rooms.filter((room) => {
      return (
        room.roomNumber.toLowerCase().includes(term) ||
        (room.roomTypeName ?? "").toLowerCase().includes(term)
      );
    });
  }, [rooms, search]);

  const metrics = useMemo(() => {
    const statusCounts = rooms.reduce<Record<string, number>>((acc, room) => {
      const label = getRoomStatusLabel(room.currentStatus);
      acc[label] = (acc[label] ?? 0) + 1;
      return acc;
    }, {});

    return [
      { label: "Total rooms", value: String(rooms.length) },
      { label: "Occupied", value: String(statusCounts["Occupied"] ?? 0) },
      { label: "Needs cleaning", value: String(statusCounts["NeedsCleaning"] ?? 0) }
    ];
  }, [rooms]);

  const openCreateModal = () => {
    if (roomTypes.length === 0) {
      setRoomTypeModalOpen(true);
      return;
    }
    setEditingRoom(null);
    setFormData({
      ...emptyFormData,
      roomTypeId: roomTypes[0]?.id ?? ""
    });
    setModalOpen(true);
  };

  const openEditModal = (room: RoomDto) => {
    setEditingRoom(room);
    setFormData({
      roomNumber: room.roomNumber,
      roomTypeId: room.roomTypeId,
      isActive: room.isActive,
      currentStatus: typeof room.currentStatus === "number" ? room.currentStatus : 0
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditingRoom(null);
    setFormData(emptyFormData);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editingRoom) {
      updateMutation.mutate({
        id: editingRoom.id,
        request: {
          roomNumber: formData.roomNumber,
          roomTypeId: formData.roomTypeId,
          isActive: formData.isActive,
          currentStatus: formData.currentStatus
        }
      });
    } else {
      createMutation.mutate({
        roomNumber: formData.roomNumber,
        roomTypeId: formData.roomTypeId
      });
    }
  };

  const handleRoomTypeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createRoomTypeMutation.mutate({
      name: roomTypeForm.name,
      description: roomTypeForm.description || undefined,
      capacity: roomTypeForm.capacity,
      basePrice: roomTypeForm.basePrice
    });
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Inventory</p>
          <h2 className="text-2xl font-semibold text-slate-900">Rooms board</h2>
          <p className="mt-1 text-sm text-slate-500">Status visibility for managers.</p>
        </div>
        <div className="flex items-center gap-3">
          <label className="flex items-center gap-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
            <input
              type="checkbox"
              checked={activeOnly}
              onChange={(event) => setActiveOnly(event.target.checked)}
            />
            Active only
          </label>
          {isManager && (
            <>
              <button
                onClick={() => setRoomTypeModalOpen(true)}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600 hover:bg-slate-50"
              >
                Add Type
              </button>
              <button
                onClick={openCreateModal}
                className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
              >
                Add room
              </button>
            </>
          )}
        </div>
      </header>

      {/* Room Types Warning */}
      {roomTypes.length === 0 && !roomTypesQuery.isLoading && (
        <div className="rounded-2xl border border-amber-200 bg-amber-50 p-4">
          <p className="text-sm font-semibold text-amber-800">No room types configured</p>
          <p className="mt-1 text-sm text-amber-700">
            You need to create at least one room type before you can add rooms.{" "}
            <button
              onClick={() => setRoomTypeModalOpen(true)}
              className="font-semibold underline"
            >
              Create a room type
            </button>
          </p>
        </div>
      )}

      {/* Room Types List */}
      {roomTypes.length > 0 && (
        <section className="panel p-4">
          <h3 className="text-sm font-semibold text-slate-700">Room Types</h3>
          <div className="mt-2 flex flex-wrap gap-2">
            {roomTypes.map((type: RoomTypeDto) => (
              <span
                key={type.id}
                className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1 text-xs text-slate-600"
              >
                {type.name} (Cap: {type.capacity}, Base: €{type.basePrice})
              </span>
            ))}
          </div>
        </section>
      )}

      <section className="grid gap-4 md:grid-cols-3">
        {metrics.map((metric) => (
          <div key={metric.label} className="panel px-4 py-5">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-400">{metric.label}</p>
            <p className="mt-3 text-3xl font-semibold text-slate-900">{metric.value}</p>
          </div>
        ))}
      </section>

      <section className="panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-900">Room status</h3>
          <div className="flex items-center gap-2">
            <input
              placeholder="Search room"
              value={search}
              onChange={(event) => setSearch(event.target.value)}
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
            />
          </div>
        </div>
        <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-[0.2em] text-slate-400">
              <tr>
                <th className="px-4 py-3">Room</th>
                <th className="px-4 py-3">Type</th>
                <th className="px-4 py-3">Status</th>
                <th className="px-4 py-3">Active</th>
                <th className="px-4 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody>
              {roomsQuery.isLoading && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-slate-500">
                    Loading rooms...
                  </td>
                </tr>
              )}
              {roomsQuery.isError && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-rose-500">
                    Failed to load rooms. Check your token or API.
                  </td>
                </tr>
              )}
              {!roomsQuery.isLoading && !roomsQuery.isError && filteredRooms.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-slate-500">
                    No rooms match the current filter.
                  </td>
                </tr>
              )}
              {filteredRooms.map((room: RoomDto) => (
                <tr key={room.id} className="border-t border-slate-100">
                  <td className="px-4 py-3 font-semibold text-slate-900">{room.roomNumber}</td>
                  <td className="px-4 py-3 text-slate-600">{room.roomTypeName ?? "—"}</td>
                  <td className="px-4 py-3">
                    <StatusPill status={getRoomStatusLabel(room.currentStatus)} />
                  </td>
                  <td className="px-4 py-3 text-slate-600">{room.isActive ? "Yes" : "No"}</td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      {isManager && (
                        <>
                          <button
                            onClick={() => openEditModal(room)}
                            className="rounded-full border border-slate-200 px-3 py-1 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50"
                          >
                            Edit
                          </button>
                          <button
                            onClick={() => setDeleteConfirm(room)}
                            className="rounded-full border border-rose-200 px-3 py-1 text-xs font-semibold uppercase text-rose-600 hover:bg-rose-50"
                          >
                            Delete
                          </button>
                        </>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* Create/Edit Room Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">
              {editingRoom ? "Edit Room" : "Add Room"}
            </h3>
            <form onSubmit={handleSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Room Number
                </label>
                <input
                  type="text"
                  value={formData.roomNumber}
                  onChange={(e) => setFormData({ ...formData, roomNumber: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Room Type
                </label>
                <select
                  value={formData.roomTypeId}
                  onChange={(e) => setFormData({ ...formData, roomTypeId: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                >
                  <option value="">Select a type</option>
                  {roomTypes.map((type: RoomTypeDto) => (
                    <option key={type.id} value={type.id}>
                      {type.name} (Capacity: {type.capacity})
                    </option>
                  ))}
                </select>
              </div>
              {editingRoom && (
                <>
                  <div>
                    <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                      Status
                    </label>
                    <select
                      value={formData.currentStatus}
                      onChange={(e) =>
                        setFormData({ ...formData, currentStatus: Number(e.target.value) })
                      }
                      className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                    >
                      {ROOM_STATUS_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      id="isActive"
                      checked={formData.isActive}
                      onChange={(e) => setFormData({ ...formData, isActive: e.target.checked })}
                    />
                    <label
                      htmlFor="isActive"
                      className="text-xs font-semibold uppercase tracking-[0.2em] text-slate-500"
                    >
                      Active
                    </label>
                  </div>
                </>
              )}
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
                  disabled={isPending}
                  className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {isPending ? "Saving..." : editingRoom ? "Update" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Room Type Modal */}
      {roomTypeModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Add Room Type</h3>
            <form onSubmit={handleRoomTypeSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Name *
                </label>
                <input
                  type="text"
                  value={roomTypeForm.name}
                  onChange={(e) => setRoomTypeForm({ ...roomTypeForm, name: e.target.value })}
                  placeholder="e.g., Standard, Deluxe, Suite"
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Description
                </label>
                <input
                  type="text"
                  value={roomTypeForm.description}
                  onChange={(e) => setRoomTypeForm({ ...roomTypeForm, description: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                />
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Capacity *
                  </label>
                  <input
                    type="number"
                    min="1"
                    value={roomTypeForm.capacity}
                    onChange={(e) =>
                      setRoomTypeForm({ ...roomTypeForm, capacity: Number(e.target.value) })
                    }
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                    required
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Base Price (€)
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    value={roomTypeForm.basePrice}
                    onChange={(e) =>
                      setRoomTypeForm({ ...roomTypeForm, basePrice: Number(e.target.value) })
                    }
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  />
                </div>
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => {
                    setRoomTypeModalOpen(false);
                    setRoomTypeForm(emptyRoomTypeForm);
                  }}
                  className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={createRoomTypeMutation.isPending}
                  className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {createRoomTypeMutation.isPending ? "Creating..." : "Create Type"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Delete Room</h3>
            <p className="mt-2 text-sm text-slate-600">
              Are you sure you want to delete room{" "}
              <span className="font-semibold">{deleteConfirm.roomNumber}</span>? This action cannot
              be undone.
            </p>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteMutation.mutate(deleteConfirm.id)}
                disabled={deleteMutation.isPending}
                className="rounded-full bg-rose-600 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
              >
                {deleteMutation.isPending ? "Deleting..." : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
