type RoomStatusLabel =
  | "Available"
  | "Occupied"
  | "NeedsCleaning"
  | "CleaningInProgress"
  | "OutOfService"
  | "Maintenance"
  | "Unknown"
  | string;

export default function StatusPill({ status }: { status: RoomStatusLabel }) {
  const styles: Record<string, string> = {
    Available: "bg-emerald-100 text-emerald-700",
    Occupied: "bg-slate-900 text-white",
    NeedsCleaning: "bg-amber-100 text-amber-700",
    CleaningInProgress: "bg-amber-200 text-amber-800",
    OutOfService: "bg-rose-100 text-rose-700",
    Maintenance: "bg-violet-100 text-violet-700"
  };

  return (
    <span className={`badge ${styles[status] ?? "bg-slate-100 text-slate-600"}`}>{status}</span>
  );
}
