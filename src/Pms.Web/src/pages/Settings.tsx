import { useState, useEffect } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getBrandingFull, updateBranding, getBrandingHistory } from "../api/branding";
import type { UpdateBrandingRequest, BrandingChangeLogDto } from "../api/types";

const formatDateTime = (dateStr: string) => {
  return new Date(dateStr).toLocaleString("fi-FI");
};

type ChangedField = {
  field: string;
  from: string;
  to: string;
};

const fieldLabels: Record<string, string> = {
  CompanyName: "Company Name",
  CompanyLegalName: "Legal Name",
  Tagline: "Tagline",
  Address: "Address",
  Email: "Email",
  Phone: "Phone",
  TaxId: "Tax ID",
  BankName: "Bank Name",
  IBAN: "IBAN",
  BIC: "BIC"
};

const parseChanges = (entry: BrandingChangeLogDto): ChangedField[] => {
  if (!entry.oldValues || !entry.newValues) return [];
  try {
    const oldObj = JSON.parse(entry.oldValues) as Record<string, string>;
    const newObj = JSON.parse(entry.newValues) as Record<string, string>;
    const changes: ChangedField[] = [];
    for (const key of Object.keys(newObj)) {
      if (oldObj[key] !== newObj[key]) {
        changes.push({
          field: fieldLabels[key] || key,
          from: oldObj[key] || "",
          to: newObj[key] || ""
        });
      }
    }
    return changes;
  } catch {
    return [];
  }
};

export default function Settings() {
  const queryClient = useQueryClient();
  const [showHistory, setShowHistory] = useState(false);
  const [successMessage, setSuccessMessage] = useState("");

  // Form state
  const [companyName, setCompanyName] = useState("");
  const [companyLegalName, setCompanyLegalName] = useState("");
  const [tagline, setTagline] = useState("");
  const [address, setAddress] = useState("");
  const [email, setEmail] = useState("");
  const [phone, setPhone] = useState("");
  const [taxId, setTaxId] = useState("");
  const [bankName, setBankName] = useState("");
  const [iban, setIban] = useState("");
  const [bic, setBic] = useState("");

  const brandingQuery = useQuery({
    queryKey: ["branding-full"],
    queryFn: getBrandingFull
  });

  const historyQuery = useQuery({
    queryKey: ["branding-history"],
    queryFn: getBrandingHistory,
    enabled: showHistory
  });

  const updateMutation = useMutation({
    mutationFn: (request: UpdateBrandingRequest) => updateBranding(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["branding-full"] });
      queryClient.invalidateQueries({ queryKey: ["branding-history"] });
      setSuccessMessage("Settings saved successfully.");
      setTimeout(() => setSuccessMessage(""), 3000);
    }
  });

  // Populate form when data loads
  useEffect(() => {
    if (brandingQuery.data) {
      const d = brandingQuery.data;
      setCompanyName(d.companyName);
      setCompanyLegalName(d.companyLegalName);
      setTagline(d.tagline);
      setAddress(d.address);
      setEmail(d.email);
      setPhone(d.phone);
      setTaxId(d.taxId);
      setBankName(d.bankName);
      setIban(d.iban);
      setBic(d.bic);
    }
  }, [brandingQuery.data]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateMutation.mutate({
      companyName,
      companyLegalName,
      tagline,
      address,
      email,
      phone,
      taxId,
      bankName,
      iban,
      bic
    });
  };

  if (brandingQuery.isLoading) {
    return (
      <div className="space-y-6">
        <div>
          <h2 className="text-2xl font-semibold text-slate-900">Settings</h2>
          <p className="text-sm text-slate-500">Loading...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div>
        <h2 className="text-2xl font-semibold text-slate-900">Settings</h2>
        <p className="text-sm text-slate-500">Manage branding and business information</p>
      </div>

      {successMessage && (
        <div className="rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
          {successMessage}
        </div>
      )}

      {updateMutation.isError && (
        <div className="rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-700">
          Failed to save settings. Please check your input and try again.
        </div>
      )}

      <form onSubmit={handleSubmit} className="panel p-6 space-y-6">
        <h3 className="text-lg font-semibold text-slate-900">Branding</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Company Name *</label>
            <input
              type="text"
              value={companyName}
              onChange={(e) => setCompanyName(e.target.value)}
              required
              maxLength={200}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Legal Name *</label>
            <input
              type="text"
              value={companyLegalName}
              onChange={(e) => setCompanyLegalName(e.target.value)}
              required
              maxLength={200}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Tagline</label>
          <input
            type="text"
            value={tagline}
            onChange={(e) => setTagline(e.target.value)}
            maxLength={500}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Address</label>
          <input
            type="text"
            value={address}
            onChange={(e) => setAddress(e.target.value)}
            maxLength={500}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              maxLength={200}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Phone</label>
            <input
              type="text"
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              maxLength={50}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-medium text-slate-700 mb-1">Tax ID</label>
          <input
            type="text"
            value={taxId}
            onChange={(e) => setTaxId(e.target.value)}
            maxLength={50}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>

        <h3 className="text-lg font-semibold text-slate-900 pt-2">Bank Details</h3>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Bank Name</label>
            <input
              type="text"
              value={bankName}
              onChange={(e) => setBankName(e.target.value)}
              maxLength={200}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">IBAN</label>
            <input
              type="text"
              value={iban}
              onChange={(e) => setIban(e.target.value)}
              maxLength={50}
              className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
            />
          </div>
        </div>

        <div className="max-w-[calc(50%-0.5rem)]">
          <label className="block text-sm font-medium text-slate-700 mb-1">BIC / SWIFT</label>
          <input
            type="text"
            value={bic}
            onChange={(e) => setBic(e.target.value)}
            maxLength={20}
            className="w-full rounded-lg border border-slate-300 px-3 py-2 text-sm focus:border-slate-500 focus:outline-none"
          />
        </div>

        <div className="flex items-center gap-4 pt-2">
          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="rounded-full bg-slate-900 px-6 py-2 text-sm font-semibold text-white hover:bg-slate-800 disabled:opacity-50"
          >
            {updateMutation.isPending ? "Saving..." : "Save Changes"}
          </button>
          {brandingQuery.data && (
            <span className="text-xs text-slate-400">
              Last updated: {formatDateTime(brandingQuery.data.updatedAtUtc)}
            </span>
          )}
        </div>
      </form>

      {/* Change History */}
      <div className="panel p-6 space-y-4">
        <div className="flex items-center justify-between">
          <h3 className="text-lg font-semibold text-slate-900">Change History</h3>
          <button
            onClick={() => setShowHistory(!showHistory)}
            className="text-sm text-slate-500 hover:text-slate-700"
          >
            {showHistory ? "Hide" : "Show"}
          </button>
        </div>

        {showHistory && (
          <>
            {historyQuery.isLoading && (
              <p className="text-sm text-slate-500">Loading history...</p>
            )}

            {historyQuery.data && historyQuery.data.length === 0 && (
              <p className="text-sm text-slate-500">No changes recorded yet.</p>
            )}

            {historyQuery.data && historyQuery.data.length > 0 && (
              <div className="space-y-3">
                {historyQuery.data.map((entry) => {
                  const changes = parseChanges(entry);
                  if (changes.length === 0) return null;
                  return (
                    <div
                      key={entry.id}
                      className="rounded-xl border border-slate-200 bg-slate-50 p-4 space-y-2"
                    >
                      <div className="flex items-center justify-between">
                        <span className="text-xs text-slate-500">
                          {formatDateTime(entry.createdAtUtc)}
                        </span>
                        {entry.performedByKeycloakId && (
                          <span className="text-xs text-slate-400">
                            by {entry.performedByKeycloakId}
                          </span>
                        )}
                      </div>
                      <div className="space-y-1">
                        {changes.map((change, i) => (
                          <div key={i} className="text-sm">
                            <span className="font-medium text-slate-700">{change.field}:</span>{" "}
                            <span className="text-red-600 line-through">{change.from}</span>{" "}
                            <span className="text-emerald-600">{change.to}</span>
                          </div>
                        ))}
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
