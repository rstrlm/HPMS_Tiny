import { useEffect, useMemo, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { getRoomBlocks, getRooms } from "../api/rooms";
import { formatLocalDateTime, toUtcIsoFromDateInput } from "../lib/datetime";
import { getRoomBlockLabel } from "../lib/room";

export default function RoomBlocks() {
  const [selectedRoomId, setSelectedRoomId] = useState<string | undefined>();
  const [fromDate, setFromDate] = useState<string>("");
  const [toDate, setToDate] = useState<string>("");

  const roomsQuery = useQuery({
    queryKey: ["rooms", { activeOnly: false }],
    queryFn: () => getRooms(false)
  });

  useEffect(() => {
    if (!selectedRoomId && roomsQuery.data && roomsQuery.data.length > 0) {
      setSelectedRoomId(roomsQuery.data[0].id);
    }
  }, [roomsQuery.data, selectedRoomId]);

  const blockQuery = useQuery({
    queryKey: ["roomBlocks", { roomId: selectedRoomId, fromDate, toDate }],
    queryFn: () =>
      getRoomBlocks(
        selectedRoomId!,
        toUtcIsoFromDateInput(fromDate),
        toUtcIsoFromDateInput(toDate)
      ),
    enabled: Boolean(selectedRoomId)
  });

  const blocks = blockQuery.data ?? [];

  const roomOptions = useMemo(() => roomsQuery.data ?? [], [roomsQuery.data]);

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Operations</p>
          <h2 className="text-2xl font-semibold text-slate-900">Room blocks</h2>
          <p className="mt-1 text-sm text-slate-500">Maintenance and OutOfService windows. Manager view for now.</p>
        </div>
        <div className="flex flex-wrap items-center gap-2">
          <button className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white">
            Create block
          </button>
        </div>
      </header>

      <section className="panel p-6">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold text-slate-900">Active blocks</h3>
          <div className="flex flex-wrap items-center gap-2 text-sm">
            <select
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
              value={selectedRoomId ?? ""}
              onChange={(event) => setSelectedRoomId(event.target.value)}
            >
              {roomOptions.map((room) => (
                <option key={room.id} value={room.id}>
                  {room.roomNumber} {room.roomTypeName ? `· ${room.roomTypeName}` : ""}
                </option>
              ))}
            </select>
            <input
              type="date"
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
            />
            <input
              type="date"
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
            />
          </div>
        </div>
        <div className="mt-4 grid gap-3">
          {roomsQuery.isLoading && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
              Loading rooms…
            </div>
          )}
          {roomsQuery.isError && (
            <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-6 text-sm text-rose-600">
              Failed to load rooms. Check your token or API.
            </div>
          )}
          {blockQuery.isLoading && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
              Loading blocks…
            </div>
          )}
          {blockQuery.isError && (
            <div className="rounded-2xl border border-rose-200 bg-rose-50 px-4 py-6 text-sm text-rose-600">
              Failed to load blocks. Check your token or API.
            </div>
          )}
          {!blockQuery.isLoading && !blockQuery.isError && blocks.length === 0 && (
            <div className="rounded-2xl border border-slate-200 bg-white px-4 py-6 text-sm text-slate-500">
              No blocks found for the selected room and range.
            </div>
          )}
          {blocks.map((block) => (
            <div
              key={block.id}
              className="flex flex-wrap items-center justify-between gap-4 rounded-2xl border border-slate-200 bg-slate-50 px-4 py-4"
            >
              <div>
                <p className="text-xs uppercase tracking-[0.2em] text-slate-400">
                  Room {block.roomNumber ?? "—"}
                </p>
                <p className="text-lg font-semibold text-slate-900">{getRoomBlockLabel(block.type)}</p>
                <p className="text-sm text-slate-500">{block.note ?? "No notes"}</p>
              </div>
              <div className="text-right">
                <p className="text-sm font-medium text-slate-700">{formatLocalDateTime(block.startAtUtc)}</p>
                <p className="text-xs text-slate-500">to {formatLocalDateTime(block.endAtUtc)}</p>
              </div>
              <button className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600">
                Edit
              </button>
            </div>
          ))}
        </div>
      </section>
    </div>
  );
}
