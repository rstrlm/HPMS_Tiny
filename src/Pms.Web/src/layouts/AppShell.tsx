import { Outlet } from "react-router-dom";
import Sidebar from "../components/Sidebar";
import TopBar from "../components/TopBar";
import MobileNav from "../components/MobileNav";

export default function AppShell() {
  return (
    <div className="app-grid">
      <Sidebar />
      <div className="min-h-screen">
        <TopBar />
        <MobileNav />
        <main className="px-8 py-8">
          <div className="animate-enter">
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
