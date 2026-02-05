import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { handleCallback } from "../lib/oidc";

export default function Callback() {
  const navigate = useNavigate();
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const processCallback = async () => {
      try {
        await handleCallback();
        navigate("/dashboard", { replace: true });
      } catch (err) {
        console.error("Callback error:", err);
        setError(err instanceof Error ? err.message : "Authentication failed");
      }
    };

    processCallback();
  }, [navigate]);

  if (error) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="panel mx-auto max-w-md p-8 text-center">
          <p className="text-xs uppercase tracking-[0.3em] text-rose-400">Error</p>
          <h2 className="mt-2 text-2xl font-semibold text-slate-900">Authentication Failed</h2>
          <p className="mt-2 text-sm text-slate-500">{error}</p>
          <button
            onClick={() => navigate("/", { replace: true })}
            className="mt-4 rounded-full bg-slate-900 px-6 py-2 text-sm font-semibold text-white"
          >
            Go Home
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="flex min-h-screen items-center justify-center">
      <div className="panel mx-auto max-w-md p-8 text-center">
        <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Please wait</p>
        <h2 className="mt-2 text-2xl font-semibold text-slate-900">Signing in...</h2>
        <p className="mt-2 text-sm text-slate-500">Processing authentication</p>
      </div>
    </div>
  );
}
