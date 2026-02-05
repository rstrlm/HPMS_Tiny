export default function StatCard({
  label,
  value,
  tone
}: {
  label: string;
  value: string;
  tone: "tide" | "ember" | "moss";
}) {
  const toneMap: Record<string, string> = {
    tide: "bg-cyan-50 text-cyan-800 border-cyan-100",
    ember: "bg-amber-50 text-amber-800 border-amber-100",
    moss: "bg-emerald-50 text-emerald-800 border-emerald-100"
  };

  return (
    <div className={`rounded-2xl border px-4 py-4 ${toneMap[tone]}`}>
      <p className="text-xs uppercase tracking-[0.2em]">{label}</p>
      <p className="mt-3 text-3xl font-semibold">{value}</p>
    </div>
  );
}
