import { NavLink } from "react-router-dom";
import { hasAnyRole, isDevModeEnabled, Role, useAuth } from "../state/auth";
import { useBranding } from "../state/branding";

const navItems: { label: string; to: string; roles: Role[] }[] = [
  { label: "Dashboard", to: "/dashboard", roles: [] },
  { label: "Rooms", to: "/rooms", roles: ["manager"] },
  { label: "Room Blocks", to: "/rooms/blocks", roles: ["manager"] },
  { label: "Customers", to: "/customers", roles: ["manager", "frontdesk"] },
  { label: "Reservations", to: "/reservations", roles: ["manager", "frontdesk"] },
  { label: "Billing", to: "/billing", roles: ["manager", "frontdesk"] },
  { label: "Housekeeping", to: "/housekeeping", roles: ["manager", "cleaner"] },
  { label: "Treatments", to: "/treatments", roles: ["manager", "therapist"] },
  { label: "Staff", to: "/staff", roles: ["manager"] },
  { label: "Settings", to: "/settings", roles: ["manager"] }
];

export default function Sidebar() {
  const { roles, isAuthenticated, displayName } = useAuth();
  const isDevMode = isDevModeEnabled();
  const { companyName } = useBranding();

  return (
    <aside className="hidden md:flex flex-col gap-6 px-6 py-8 border-r border-slate-200 bg-white/80 glass">
      <div className="space-y-2">
        <p className="text-xs uppercase tracking-[0.25em] text-slate-500">PMS</p>
        <h1 className="text-2xl font-semibold text-slate-900">{companyName}</h1>
        <p className="text-sm text-slate-500">Ops console</p>
      </div>
      <nav className="flex flex-col gap-2">
        {navItems
          .filter((item) => hasAnyRole(roles, item.roles))
          .map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              className={({ isActive }) =>
                [
                  "rounded-xl px-4 py-3 text-sm font-medium transition",
                  isActive
                    ? "bg-slate-900 text-white shadow-soft"
                    : "text-slate-600 hover:bg-slate-100 hover:text-slate-900"
                ].join(" ")
              }
            >
              {item.label}
            </NavLink>
          ))}
      </nav>
      <div className="mt-auto space-y-3">
        <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
          <p className="text-xs uppercase text-slate-400">Active Roles</p>
          <p className="text-sm font-medium text-slate-700">{roles.length ? roles.join(", ") : "None"}</p>
        </div>
        {isDevMode ? (
          <div className="rounded-2xl border border-amber-200 bg-amber-50 px-4 py-3">
            <p className="text-xs uppercase text-amber-500">Dev Mode</p>
            <p className="text-sm text-amber-700">Use role switcher in top bar</p>
          </div>
        ) : isAuthenticated ? (
          <div className="rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3">
            <p className="text-xs uppercase text-emerald-500">Authenticated</p>
            <p className="text-sm text-emerald-700">{displayName ?? "Signed in"}</p>
          </div>
        ) : (
          <div className="rounded-2xl border border-slate-200 bg-slate-50 px-4 py-3">
            <p className="text-xs uppercase text-slate-400">Not signed in</p>
            <p className="text-sm text-slate-600">Click Login to authenticate</p>
          </div>
        )}
      </div>
    </aside>
  );
}
