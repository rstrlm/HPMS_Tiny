import { Navigate, Route, Routes } from "react-router-dom";
import AppShell from "./layouts/AppShell";
import Dashboard from "./pages/Dashboard";
import Rooms from "./pages/Rooms";
import RoomBlocks from "./pages/RoomBlocks";
import Customers from "./pages/Customers";
import Reservations from "./pages/Reservations";
import Billing from "./pages/Billing";
import Housekeeping from "./pages/Housekeeping";
import TherapistSchedule from "./pages/TherapistSchedule";
import Staff from "./pages/Staff";
import Settings from "./pages/Settings";
import Unauthorized from "./pages/Unauthorized";
import NotFound from "./pages/NotFound";
import Callback from "./pages/Callback";
import { Role, hasAnyRole, useAuth } from "./state/auth";
import { useBranding } from "./state/branding";

const RequireRole = ({ allow, children }: { allow: Role[]; children: JSX.Element }) => {
  const { roles } = useAuth();
  if (!hasAnyRole(roles, allow)) {
    return <Navigate to="/unauthorized" replace />;
  }
  return children;
};

const LoadingScreen = () => {
  const { companyName } = useBranding();
  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="panel mx-auto max-w-md p-8 text-center">
        <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Loading</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">{companyName} PMS</h2>
        <p className="mt-2 text-sm text-slate-500">Checking authentication...</p>
      </div>
    </div>
  );
};

export default function App() {
  const { isLoading } = useAuth();

  if (isLoading) {
    return <LoadingScreen />;
  }

  return (
    <Routes>
      <Route path="/callback" element={<Callback />} />
      <Route path="/" element={<AppShell />}>
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="dashboard" element={<Dashboard />} />
        <Route
          path="rooms"
          element={
            <RequireRole allow={["manager"]}>
              <Rooms />
            </RequireRole>
          }
        />
        <Route
          path="rooms/blocks"
          element={
            <RequireRole allow={["manager"]}>
              <RoomBlocks />
            </RequireRole>
          }
        />
        <Route
          path="customers"
          element={
            <RequireRole allow={["manager", "frontdesk"]}>
              <Customers />
            </RequireRole>
          }
        />
        <Route
          path="reservations"
          element={
            <RequireRole allow={["manager", "frontdesk"]}>
              <Reservations />
            </RequireRole>
          }
        />
        <Route
          path="billing"
          element={
            <RequireRole allow={["manager", "frontdesk"]}>
              <Billing />
            </RequireRole>
          }
        />
        <Route
          path="housekeeping"
          element={
            <RequireRole allow={["manager", "cleaner"]}>
              <Housekeeping />
            </RequireRole>
          }
        />
        <Route
          path="treatments"
          element={
            <RequireRole allow={["manager", "therapist"]}>
              <TherapistSchedule />
            </RequireRole>
          }
        />
        <Route
          path="staff"
          element={
            <RequireRole allow={["manager"]}>
              <Staff />
            </RequireRole>
          }
        />
        <Route
          path="settings"
          element={
            <RequireRole allow={["manager"]}>
              <Settings />
            </RequireRole>
          }
        />
        <Route path="unauthorized" element={<Unauthorized />} />
        <Route path="*" element={<NotFound />} />
      </Route>
    </Routes>
  );
}
