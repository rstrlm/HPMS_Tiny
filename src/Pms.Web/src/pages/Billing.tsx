import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import StatusPill from "../components/StatusPill";
import {
  getFolio,
  getFoliosByCustomer,
  createFolio,
  addCharge,
  addPayment,
  issueInvoice,
  closeFolio,
  cancelFolio,
  mergeFolios,
  getInvoicesByFolio,
  downloadInvoicePdf
} from "../api/billing";
import { getCustomers } from "../api/customers";
import type {
  FolioSummaryDto,
  CustomerDto,
  CreateChargeRequest,
  CreatePaymentRequest
} from "../api/types";
import { hasAnyRole, useAuth } from "../state/auth";

const CHARGE_TYPE_OPTIONS = [
  { value: 0, label: "Room Night" },
  { value: 1, label: "Treatment" },
  { value: 2, label: "Custom" }
];

const PAYMENT_METHOD_OPTIONS = [
  { value: 0, label: "Cash" },
  { value: 1, label: "Card" },
  { value: 2, label: "Online" }
];

const getFolioStatusLabel = (status: number | string) => {
  if (status === 0 || status === "0") return "Open";
  if (status === 1 || status === "1") return "Closed";
  if (status === 2 || status === "2") return "Cancelled";
  return "Unknown";
};

const getInvoiceStatusLabel = (status: number | string) => {
  if (status === 0 || status === "0") return "Issued";
  if (status === 1 || status === "1") return "Voided";
  return "Unknown";
};

const getChargeTypeLabel = (type: number | string) => {
  return CHARGE_TYPE_OPTIONS.find((o) => o.value === type || String(o.value) === type)?.label ?? "Custom";
};

const getPaymentMethodLabel = (method: number | string) => {
  return PAYMENT_METHOD_OPTIONS.find((o) => o.value === method || String(o.value) === method)?.label ?? "Other";
};

const formatCurrency = (amount: number) => {
  return new Intl.NumberFormat("fi-FI", { style: "currency", currency: "EUR" }).format(amount);
};

export default function Billing() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const queryClient = useQueryClient();

  const [selectedCustomerId, setSelectedCustomerId] = useState<string>("");
  const [selectedFolioId, setSelectedFolioId] = useState<string | null>(null);
  const [chargeModalOpen, setChargeModalOpen] = useState(false);
  const [paymentModalOpen, setPaymentModalOpen] = useState(false);
  const [createFolioModalOpen, setCreateFolioModalOpen] = useState(false);
  const [mergeModalOpen, setMergeModalOpen] = useState(false);
  const [selectedFoliosForMerge, setSelectedFoliosForMerge] = useState<string[]>([]);

  // Form states
  const [chargeForm, setChargeForm] = useState<CreateChargeRequest>({
    type: 2,
    description: "",
    quantity: 1,
    unitPrice: 0,
    vatRate: 0.24
  });
  const [priceIncludesVat, setPriceIncludesVat] = useState(true); // Default to VAT-inclusive (Finnish standard)
  const [enteredPrice, setEnteredPrice] = useState(0); // The price user enters (may or may not include VAT)
  const [paymentForm, setPaymentForm] = useState<CreatePaymentRequest>({
    amount: 0,
    method: 0
  });

  // Calculate unit price (net) from entered price based on VAT-inclusive mode
  const calculateNetPrice = (priceEntered: number, vatRate: number, includesVat: boolean) => {
    if (includesVat) {
      // Price includes VAT, calculate net price: net = gross / (1 + vatRate)
      return priceEntered / (1 + vatRate);
    }
    // Price is already net
    return priceEntered;
  };

  // Calculate preview values
  const previewUnitPrice = calculateNetPrice(enteredPrice, chargeForm.vatRate ?? 0.24, priceIncludesVat);
  const previewVat = previewUnitPrice * (chargeForm.vatRate ?? 0.24);
  const previewTotal = previewUnitPrice + previewVat;

  // Queries
  const customersQuery = useQuery({
    queryKey: ["customers"],
    queryFn: () => getCustomers()
  });

  const foliosQuery = useQuery({
    queryKey: ["folios", { customerId: selectedCustomerId }],
    queryFn: () => getFoliosByCustomer(selectedCustomerId),
    enabled: !!selectedCustomerId
  });

  const folioDetailQuery = useQuery({
    queryKey: ["folio", selectedFolioId],
    queryFn: () => getFolio(selectedFolioId!),
    enabled: !!selectedFolioId
  });

  const invoicesQuery = useQuery({
    queryKey: ["invoices", selectedFolioId],
    queryFn: () => getInvoicesByFolio(selectedFolioId!),
    enabled: !!selectedFolioId
  });

  // Mutations
  const createFolioMutation = useMutation({
    mutationFn: () => createFolio({ customerId: selectedCustomerId }),
    onSuccess: (folio) => {
      queryClient.invalidateQueries({ queryKey: ["folios"] });
      setSelectedFolioId(folio.id);
      setCreateFolioModalOpen(false);
    }
  });

  const addChargeMutation = useMutation({
    mutationFn: (request: CreateChargeRequest) => addCharge(selectedFolioId!, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folio", selectedFolioId] });
      queryClient.invalidateQueries({ queryKey: ["folios"] });
      setChargeModalOpen(false);
      setChargeForm({ type: 2, description: "", quantity: 1, unitPrice: 0, vatRate: 0.24 });
      setEnteredPrice(0);
    }
  });

  const addPaymentMutation = useMutation({
    mutationFn: (request: CreatePaymentRequest) => addPayment(selectedFolioId!, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folio", selectedFolioId] });
      queryClient.invalidateQueries({ queryKey: ["folios"] });
      setPaymentModalOpen(false);
      setPaymentForm({ amount: 0, method: 0 });
    }
  });

  const issueInvoiceMutation = useMutation({
    mutationFn: () => issueInvoice(selectedFolioId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folio", selectedFolioId] });
    }
  });

  const closeFolioMutation = useMutation({
    mutationFn: () => closeFolio(selectedFolioId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folio", selectedFolioId] });
      queryClient.invalidateQueries({ queryKey: ["folios"] });
    }
  });

  const cancelFolioMutation = useMutation({
    mutationFn: () => cancelFolio(selectedFolioId!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["folio", selectedFolioId] });
      queryClient.invalidateQueries({ queryKey: ["folios"] });
    }
  });

  const mergeFoliosMutation = useMutation({
    mutationFn: ({ targetId, sourceIds }: { targetId: string; sourceIds: string[] }) =>
      mergeFolios(targetId, sourceIds),
    onSuccess: (folio) => {
      queryClient.invalidateQueries({ queryKey: ["folios"] });
      queryClient.invalidateQueries({ queryKey: ["folio"] });
      setSelectedFolioId(folio.id);
      setMergeModalOpen(false);
      setSelectedFoliosForMerge([]);
    }
  });

  const customers = customersQuery.data ?? [];
  const folios = foliosQuery.data ?? [];
  const folio = folioDetailQuery.data;
  const invoices = invoicesQuery.data ?? [];

  // Get open folios for merge (excluding selected folio)
  const openFoliosForMerge = folios.filter(
    (f) => f.status === 0 && f.id !== selectedFolioId
  );

  const handleChargeSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    // Always send net price to API
    const netUnitPrice = calculateNetPrice(enteredPrice, chargeForm.vatRate ?? 0.24, priceIncludesVat);
    addChargeMutation.mutate({
      ...chargeForm,
      unitPrice: netUnitPrice
    });
  };

  const handlePaymentSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    addPaymentMutation.mutate(paymentForm);
  };

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Finance</p>
          <h2 className="text-2xl font-semibold text-slate-900">Billing</h2>
          <p className="mt-1 text-sm text-slate-500">Manage folios, charges, and payments.</p>
        </div>
      </header>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Customer Selection */}
        <section className="panel p-6">
          <h3 className="text-lg font-semibold text-slate-900">Select Customer</h3>
          <select
            value={selectedCustomerId}
            onChange={(e) => {
              setSelectedCustomerId(e.target.value);
              setSelectedFolioId(null);
            }}
            className="mt-4 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
          >
            <option value="">Choose a customer...</option>
            {customers.map((c: CustomerDto) => (
              <option key={c.id} value={c.id}>
                {c.name}
              </option>
            ))}
          </select>

          {selectedCustomerId && (
            <div className="mt-6">
              <div className="flex items-center justify-between">
                <h4 className="text-sm font-semibold text-slate-700">Folios</h4>
                <button
                  onClick={() => setCreateFolioModalOpen(true)}
                  className="text-xs font-semibold uppercase text-slate-600 hover:text-slate-900"
                >
                  + New
                </button>
              </div>
              {foliosQuery.isLoading && (
                <p className="mt-3 text-sm text-slate-500">Loading...</p>
              )}
              {folios.length === 0 && !foliosQuery.isLoading && (
                <p className="mt-3 text-sm text-slate-500">No folios found.</p>
              )}
              <div className="mt-3 space-y-2">
                {folios.map((f: FolioSummaryDto) => (
                  <button
                    key={f.id}
                    onClick={() => setSelectedFolioId(f.id)}
                    className={`w-full rounded-xl border px-4 py-3 text-left transition ${
                      selectedFolioId === f.id
                        ? "border-slate-900 bg-slate-50"
                        : "border-slate-200 hover:border-slate-300"
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <StatusPill status={getFolioStatusLabel(f.status)} />
                      <span className="text-xs text-slate-500">
                        {new Date(f.createdAtUtc).toLocaleDateString()}
                      </span>
                    </div>
                    <div className="mt-2 flex justify-between text-sm">
                      <span className="text-slate-600">Total: {formatCurrency(f.grandTotal)}</span>
                      <span
                        className={f.balance > 0 ? "font-semibold text-amber-600" : "text-emerald-600"}
                      >
                        Balance: {formatCurrency(f.balance)}
                      </span>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          )}
        </section>

        {/* Folio Detail */}
        <section className="panel p-6 lg:col-span-2">
          {!selectedFolioId ? (
            <div className="flex h-full items-center justify-center text-sm text-slate-500">
              Select a customer and folio to view details
            </div>
          ) : folioDetailQuery.isLoading ? (
            <p className="text-sm text-slate-500">Loading folio...</p>
          ) : !folio ? (
            <p className="text-sm text-rose-500">Failed to load folio</p>
          ) : (
            <>
              <div className="flex flex-wrap items-center justify-between gap-4">
                <div>
                  <div className="flex items-center gap-3">
                    <h3 className="text-lg font-semibold text-slate-900">Folio Details</h3>
                    <StatusPill status={getFolioStatusLabel(folio.status)} />
                  </div>
                  <p className="mt-1 text-sm text-slate-500">{folio.customerName}</p>
                </div>
                <div className="flex gap-2">
                  {folio.status === 0 && (
                    <>
                      <button
                        onClick={() => setChargeModalOpen(true)}
                        className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50"
                      >
                        Add Charge
                      </button>
                      <button
                        onClick={() => setPaymentModalOpen(true)}
                        className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50"
                      >
                        Add Payment
                      </button>
                      <button
                        onClick={() => issueInvoiceMutation.mutate()}
                        disabled={issueInvoiceMutation.isPending}
                        className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50 disabled:opacity-50"
                      >
                        Issue Invoice
                      </button>
                      {isManager && openFoliosForMerge.length > 0 && (
                        <button
                          onClick={() => setMergeModalOpen(true)}
                          className="rounded-full border border-slate-200 px-4 py-2 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50"
                        >
                          Merge Folios
                        </button>
                      )}
                      {isManager && folio.balance === 0 && (
                        <button
                          onClick={() => closeFolioMutation.mutate()}
                          disabled={closeFolioMutation.isPending}
                          className="rounded-full bg-emerald-600 px-4 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                        >
                          Close Folio
                        </button>
                      )}
                      {isManager && folio.totalPaid === 0 && (
                        <button
                          onClick={() => {
                            if (confirm("Are you sure you want to cancel this folio?")) {
                              cancelFolioMutation.mutate();
                            }
                          }}
                          disabled={cancelFolioMutation.isPending}
                          className="rounded-full border border-rose-200 px-4 py-2 text-xs font-semibold uppercase text-rose-600 hover:bg-rose-50 disabled:opacity-50"
                        >
                          Cancel Folio
                        </button>
                      )}
                    </>
                  )}
                </div>
              </div>

              {/* Summary */}
              <div className="mt-6 grid gap-4 md:grid-cols-4">
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <p className="text-xs uppercase text-slate-400">Subtotal</p>
                  <p className="mt-1 text-lg font-semibold">{formatCurrency(folio.subTotal)}</p>
                </div>
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <p className="text-xs uppercase text-slate-400">VAT</p>
                  <p className="mt-1 text-lg font-semibold">{formatCurrency(folio.vatTotal)}</p>
                </div>
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <p className="text-xs uppercase text-slate-400">Total</p>
                  <p className="mt-1 text-lg font-semibold">{formatCurrency(folio.grandTotal)}</p>
                </div>
                <div
                  className={`rounded-xl border px-4 py-3 ${
                    folio.balance > 0
                      ? "border-amber-200 bg-amber-50"
                      : "border-emerald-200 bg-emerald-50"
                  }`}
                >
                  <p
                    className={`text-xs uppercase ${
                      folio.balance > 0 ? "text-amber-500" : "text-emerald-500"
                    }`}
                  >
                    Balance
                  </p>
                  <p className="mt-1 text-lg font-semibold">{formatCurrency(folio.balance)}</p>
                </div>
              </div>

              {/* Charges */}
              <div className="mt-6">
                <h4 className="text-sm font-semibold text-slate-700">Charges</h4>
                {folio.charges.length === 0 ? (
                  <p className="mt-2 text-sm text-slate-500">No charges yet</p>
                ) : (
                  <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                    <table className="w-full text-left text-sm">
                      <thead className="bg-slate-50 text-xs uppercase text-slate-400">
                        <tr>
                          <th className="px-4 py-2">Description</th>
                          <th className="px-4 py-2">Type</th>
                          <th className="px-4 py-2 text-right">Qty</th>
                          <th className="px-4 py-2 text-right">Unit</th>
                          <th className="px-4 py-2 text-right">Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {folio.charges.map((charge) => (
                          <tr key={charge.id} className="border-t border-slate-100">
                            <td className="px-4 py-2 text-slate-900">{charge.description}</td>
                            <td className="px-4 py-2 text-slate-600">
                              {getChargeTypeLabel(charge.type)}
                            </td>
                            <td className="px-4 py-2 text-right text-slate-600">{charge.quantity}</td>
                            <td className="px-4 py-2 text-right text-slate-600">
                              {formatCurrency(charge.unitPrice)}
                            </td>
                            <td className="px-4 py-2 text-right font-semibold">
                              {formatCurrency(charge.total)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Payments */}
              <div className="mt-6">
                <h4 className="text-sm font-semibold text-slate-700">Payments</h4>
                {folio.payments.length === 0 ? (
                  <p className="mt-2 text-sm text-slate-500">No payments yet</p>
                ) : (
                  <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                    <table className="w-full text-left text-sm">
                      <thead className="bg-slate-50 text-xs uppercase text-slate-400">
                        <tr>
                          <th className="px-4 py-2">Date</th>
                          <th className="px-4 py-2">Method</th>
                          <th className="px-4 py-2 text-right">Amount</th>
                        </tr>
                      </thead>
                      <tbody>
                        {folio.payments.map((payment) => (
                          <tr key={payment.id} className="border-t border-slate-100">
                            <td className="px-4 py-2 text-slate-600">
                              {new Date(payment.createdAtUtc).toLocaleDateString()}
                            </td>
                            <td className="px-4 py-2 text-slate-600">
                              {getPaymentMethodLabel(payment.method)}
                            </td>
                            <td className="px-4 py-2 text-right font-semibold text-emerald-600">
                              {formatCurrency(payment.amount)}
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>

              {/* Invoices */}
              <div className="mt-6">
                <h4 className="text-sm font-semibold text-slate-700">Invoices</h4>
                {invoices.length === 0 ? (
                  <p className="mt-2 text-sm text-slate-500">No invoices issued yet</p>
                ) : (
                  <div className="mt-2 overflow-hidden rounded-xl border border-slate-200">
                    <table className="w-full text-left text-sm">
                      <thead className="bg-slate-50 text-xs uppercase text-slate-400">
                        <tr>
                          <th className="px-4 py-2">Invoice #</th>
                          <th className="px-4 py-2">Date</th>
                          <th className="px-4 py-2">Status</th>
                          <th className="px-4 py-2 text-right">Total</th>
                          <th className="px-4 py-2 text-right">Actions</th>
                        </tr>
                      </thead>
                      <tbody>
                        {invoices.map((invoice) => (
                          <tr key={invoice.id} className="border-t border-slate-100">
                            <td className="px-4 py-2 font-mono text-slate-900">
                              {invoice.invoiceNumber}
                            </td>
                            <td className="px-4 py-2 text-slate-600">
                              {new Date(invoice.issuedAtUtc).toLocaleDateString()}
                            </td>
                            <td className="px-4 py-2">
                              <StatusPill status={getInvoiceStatusLabel(invoice.status)} />
                            </td>
                            <td className="px-4 py-2 text-right font-semibold">
                              {formatCurrency(invoice.grandTotal)}
                            </td>
                            <td className="px-4 py-2 text-right">
                              <button
                                onClick={() => downloadInvoicePdf(invoice.id)}
                                className="text-xs font-semibold uppercase text-blue-600 hover:text-blue-800"
                              >
                                Download PDF
                              </button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </div>
            </>
          )}
        </section>
      </div>

      {/* Add Charge Modal */}
      {chargeModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Add Charge</h3>
            <form onSubmit={handleChargeSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Type
                </label>
                <select
                  value={chargeForm.type}
                  onChange={(e) => setChargeForm({ ...chargeForm, type: Number(e.target.value) })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                >
                  {CHARGE_TYPE_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Description *
                </label>
                <input
                  type="text"
                  value={chargeForm.description}
                  onChange={(e) => setChargeForm({ ...chargeForm, description: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  VAT Rate
                </label>
                <select
                  value={chargeForm.vatRate}
                  onChange={(e) => setChargeForm({ ...chargeForm, vatRate: Number(e.target.value) })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                >
                  <option value={0}>0%</option>
                  <option value={0.1}>10%</option>
                  <option value={0.14}>14%</option>
                  <option value={0.24}>24%</option>
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Quantity
                  </label>
                  <input
                    type="number"
                    min="1"
                    value={chargeForm.quantity}
                    onChange={(e) => setChargeForm({ ...chargeForm, quantity: Number(e.target.value) })}
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  />
                </div>
                <div>
                  <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                    Unit Price
                  </label>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    value={enteredPrice}
                    onChange={(e) => setEnteredPrice(Number(e.target.value))}
                    className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  />
                </div>
              </div>
              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="priceIncludesVat"
                  checked={priceIncludesVat}
                  onChange={(e) => setPriceIncludesVat(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300"
                />
                <label htmlFor="priceIncludesVat" className="text-sm text-slate-600">
                  Price includes VAT (Finnish standard)
                </label>
              </div>
              {enteredPrice > 0 && (
                <div className="rounded-xl border border-slate-200 bg-slate-50 px-4 py-3">
                  <p className="text-xs uppercase text-slate-400">Price Breakdown (per unit)</p>
                  <div className="mt-2 grid grid-cols-3 gap-2 text-sm">
                    <div>
                      <p className="text-slate-500">Net</p>
                      <p className="font-semibold">{formatCurrency(previewUnitPrice)}</p>
                    </div>
                    <div>
                      <p className="text-slate-500">VAT</p>
                      <p className="font-semibold">{formatCurrency(previewVat)}</p>
                    </div>
                    <div>
                      <p className="text-slate-500">Total</p>
                      <p className="font-semibold">{formatCurrency(previewTotal)}</p>
                    </div>
                  </div>
                  {chargeForm.quantity > 1 && (
                    <p className="mt-2 text-xs text-slate-500">
                      Line total: {formatCurrency(previewTotal * chargeForm.quantity)}
                    </p>
                  )}
                </div>
              )}
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setChargeModalOpen(false)}
                  className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={addChargeMutation.isPending}
                  className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {addChargeMutation.isPending ? "Adding..." : "Add Charge"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Add Payment Modal */}
      {paymentModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Add Payment</h3>
            <form onSubmit={handlePaymentSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Amount *
                </label>
                <input
                  type="number"
                  step="0.01"
                  min="0.01"
                  value={paymentForm.amount}
                  onChange={(e) => setPaymentForm({ ...paymentForm, amount: Number(e.target.value) })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                />
                {folio && (
                  <button
                    type="button"
                    onClick={() => setPaymentForm({ ...paymentForm, amount: folio.balance })}
                    className="mt-1 text-xs text-slate-500 hover:text-slate-700"
                  >
                    Pay full balance ({formatCurrency(folio.balance)})
                  </button>
                )}
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Method
                </label>
                <select
                  value={paymentForm.method}
                  onChange={(e) => setPaymentForm({ ...paymentForm, method: Number(e.target.value) })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                >
                  {PAYMENT_METHOD_OPTIONS.map((opt) => (
                    <option key={opt.value} value={opt.value}>
                      {opt.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={() => setPaymentModalOpen(false)}
                  className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={addPaymentMutation.isPending}
                  className="rounded-full bg-emerald-600 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {addPaymentMutation.isPending ? "Processing..." : "Add Payment"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Create Folio Modal */}
      {createFolioModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Create New Folio</h3>
            <p className="mt-2 text-sm text-slate-600">
              Create a new folio for{" "}
              <span className="font-semibold">
                {customers.find((c: CustomerDto) => c.id === selectedCustomerId)?.name}
              </span>
              ?
            </p>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setCreateFolioModalOpen(false)}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={() => createFolioMutation.mutate()}
                disabled={createFolioMutation.isPending}
                className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
              >
                {createFolioMutation.isPending ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Merge Folios Modal */}
      {mergeModalOpen && selectedFolioId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Merge Folios</h3>
            <p className="mt-2 text-sm text-slate-600">
              Select folios to merge into the current folio. All charges and payments will be moved.
            </p>
            <div className="mt-4 max-h-60 overflow-y-auto space-y-2">
              {openFoliosForMerge.map((f) => (
                <label
                  key={f.id}
                  className={`flex items-center gap-3 rounded-xl border px-4 py-3 cursor-pointer transition ${
                    selectedFoliosForMerge.includes(f.id)
                      ? "border-slate-900 bg-slate-50"
                      : "border-slate-200 hover:border-slate-300"
                  }`}
                >
                  <input
                    type="checkbox"
                    checked={selectedFoliosForMerge.includes(f.id)}
                    onChange={(e) => {
                      if (e.target.checked) {
                        setSelectedFoliosForMerge([...selectedFoliosForMerge, f.id]);
                      } else {
                        setSelectedFoliosForMerge(
                          selectedFoliosForMerge.filter((id) => id !== f.id)
                        );
                      }
                    }}
                    className="h-4 w-4 rounded border-slate-300"
                  />
                  <div className="flex-1">
                    <div className="flex justify-between text-sm">
                      <span className="text-slate-600">
                        Created: {new Date(f.createdAtUtc).toLocaleDateString()}
                      </span>
                      <span className="font-semibold">{formatCurrency(f.grandTotal)}</span>
                    </div>
                    {f.reservationId && (
                      <p className="text-xs text-slate-500">Has reservation</p>
                    )}
                  </div>
                </label>
              ))}
            </div>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => {
                  setMergeModalOpen(false);
                  setSelectedFoliosForMerge([]);
                }}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={() =>
                  mergeFoliosMutation.mutate({
                    targetId: selectedFolioId,
                    sourceIds: selectedFoliosForMerge
                  })
                }
                disabled={mergeFoliosMutation.isPending || selectedFoliosForMerge.length === 0}
                className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
              >
                {mergeFoliosMutation.isPending ? "Merging..." : "Merge Selected"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
