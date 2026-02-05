export default function Unauthorized() {
  return (
    <div className="panel mx-auto max-w-xl p-8 text-center">
      <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Access blocked</p>
      <h2 className="mt-2 text-2xl font-semibold text-slate-900">Not authorized</h2>
      <p className="mt-2 text-sm text-slate-500">
        Your current role does not allow this view. Use the role switcher to preview access.
      </p>
    </div>
  );
}
