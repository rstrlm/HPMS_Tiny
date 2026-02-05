import { useMemo, useState } from "react";
import { getDevRoles, isDevModeEnabled, Role, useAuth } from "../state/auth";

const allRoles: Role[] = ["manager", "cleaner", "therapist", "frontdesk", "maintenance", "accounting"];

export default function TopBar() {
  const { roles, setRoles, login, logout, isAuthenticated, displayName } = useAuth();
  const devRoles = getDevRoles();
  const [isOpen, setIsOpen] = useState(false);
  const isDevMode = isDevModeEnabled();

  const allowedRoles = useMemo(() => {
    return devRoles.length > 0 ? devRoles : allRoles;
  }, [devRoles]);

  const toggleRole = (role: Role) => {
    if (roles.includes(role)) {
      setRoles(roles.filter((r) => r !== role));
    } else {
      setRoles([...roles, role]);
    }
  };

  return (
    <header className="sticky top-0 z-10 flex items-center justify-between border-b border-slate-200 bg-white/80 px-8 py-4 glass relative">
      <div>
        <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Operations</p>
        <h2 className="text-xl font-semibold text-slate-900">Front of House</h2>
      </div>
      <div className="flex items-center gap-4">
        {/* Show user info */}
        {displayName && (
          <span className="hidden md:block text-sm text-slate-600">{displayName}</span>
        )}

        {/* Role badges */}
        <div className="hidden md:flex items-center gap-2 rounded-full bg-slate-100 px-3 py-2 text-xs font-semibold text-slate-600">
          {roles.length ? roles.join(" / ") : "No role selected"}
        </div>

        {/* Dev mode role switcher */}
        {isDevMode && (
          <button
            className="rounded-full border border-slate-200 bg-white px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
            onClick={() => setIsOpen((open) => !open)}
          >
            Roles
          </button>
        )}

        {/* Login/Logout button */}
        {!isDevMode && (
          isAuthenticated ? (
            <button
              onClick={logout}
              className="rounded-full border border-slate-200 bg-white px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
            >
              Logout
            </button>
          ) : (
            <button
              onClick={login}
              className="rounded-full bg-slate-900 px-4 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
            >
              Login
            </button>
          )
        )}

        {/* Dev role dropdown */}
        {isOpen && isDevMode && (
          <div className="absolute right-8 top-[68px] w-64 rounded-2xl border border-slate-200 bg-white p-4 shadow-panel">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Dev Roles</p>
            <div className="mt-3 grid gap-2">
              {allowedRoles.map((role) => (
                <label
                  key={role}
                  className="flex items-center justify-between rounded-xl border border-slate-200 px-3 py-2 text-sm"
                >
                  <span className="font-medium text-slate-700">{role}</span>
                  <input
                    type="checkbox"
                    checked={roles.includes(role)}
                    onChange={() => toggleRole(role)}
                  />
                </label>
              ))}
            </div>
            <p className="mt-3 text-xs text-slate-500">Stored in localStorage as pms_roles.</p>
          </div>
        )}
      </div>
    </header>
  );
}
