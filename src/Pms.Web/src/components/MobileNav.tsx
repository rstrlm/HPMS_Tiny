import { NavLink } from "react-router-dom";
import { hasAnyRole, Role, useAuth } from "../state/auth";

const navItems: { label: string; to: string; roles: Role[] }[] = [
  { label: "Dashboard", to: "/dashboard", roles: [] },
  { label: "Rooms", to: "/rooms", roles: ["manager"] },
  { label: "Blocks", to: "/rooms/blocks", roles: ["manager"] },
  { label: "Customers", to: "/customers", roles: ["manager", "frontdesk"] },
  { label: "Bookings", to: "/reservations", roles: ["manager", "frontdesk"] },
  { label: "Billing", to: "/billing", roles: ["manager", "frontdesk"] },
  { label: "Cleaning", to: "/housekeeping", roles: ["manager", "cleaner"] },
  { label: "Therapies", to: "/treatments", roles: ["manager", "therapist"] },
  { label: "Staff", to: "/staff", roles: ["manager"] },
  { label: "Settings", to: "/settings", roles: ["manager"] }
];

export default function MobileNav() {
  const { roles } = useAuth();

  return (
    <nav className="flex md:hidden gap-3 overflow-x-auto border-b border-slate-200 bg-white/80 px-4 py-3 glass">
      {navItems
        .filter((item) => hasAnyRole(roles, item.roles))
        .map((item) => (
          <NavLink
            key={item.to}
            to={item.to}
            className={({ isActive }) =>
              [
                "whitespace-nowrap rounded-full px-4 py-2 text-xs font-semibold uppercase tracking-[0.15em]",
                isActive ? "bg-slate-900 text-white" : "border border-slate-200 text-slate-600"
              ].join(" ")
            }
          >
            {item.label}
          </NavLink>
        ))}
    </nav>
  );
}
