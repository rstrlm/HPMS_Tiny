import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getCustomers, createCustomer, updateCustomer, deleteCustomer } from "../api/customers";
import type { CustomerDto, CreateCustomerRequest, UpdateCustomerRequest } from "../api/types";
import { hasAnyRole, useAuth } from "../state/auth";

type CustomerFormData = {
  name: string;
  phone: string;
  email: string;
  address: string;
  notes: string;
};

const emptyFormData: CustomerFormData = {
  name: "",
  phone: "",
  email: "",
  address: "",
  notes: ""
};

export default function Customers() {
  const { roles } = useAuth();
  const isManager = hasAnyRole(roles, ["manager"]);
  const queryClient = useQueryClient();

  const [search, setSearch] = useState("");
  const [debouncedSearch, setDebouncedSearch] = useState("");
  const [modalOpen, setModalOpen] = useState(false);
  const [editingCustomer, setEditingCustomer] = useState<CustomerDto | null>(null);
  const [formData, setFormData] = useState<CustomerFormData>(emptyFormData);
  const [deleteConfirm, setDeleteConfirm] = useState<CustomerDto | null>(null);

  // Debounce search
  const handleSearchChange = (value: string) => {
    setSearch(value);
    // Simple debounce with timeout
    setTimeout(() => {
      setDebouncedSearch(value);
    }, 300);
  };

  const customersQuery = useQuery({
    queryKey: ["customers", { q: debouncedSearch }],
    queryFn: () => getCustomers(debouncedSearch || undefined)
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateCustomerRequest) => createCustomer(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["customers"] });
      closeModal();
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateCustomerRequest }) =>
      updateCustomer(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["customers"] });
      closeModal();
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteCustomer(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["customers"] });
      setDeleteConfirm(null);
    }
  });

  const customers = customersQuery.data ?? [];

  const stats = useMemo(() => {
    return [
      { label: "Total customers", value: String(customers.length) },
      { label: "With email", value: String(customers.filter((c) => c.email).length) },
      { label: "With phone", value: String(customers.filter((c) => c.phone).length) }
    ];
  }, [customers]);

  const openCreateModal = () => {
    setEditingCustomer(null);
    setFormData(emptyFormData);
    setModalOpen(true);
  };

  const openEditModal = (customer: CustomerDto) => {
    setEditingCustomer(customer);
    setFormData({
      name: customer.name,
      phone: customer.phone ?? "",
      email: customer.email ?? "",
      address: customer.address ?? "",
      notes: customer.notes ?? ""
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditingCustomer(null);
    setFormData(emptyFormData);
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    const request = {
      name: formData.name,
      phone: formData.phone || undefined,
      email: formData.email || undefined,
      address: formData.address || undefined,
      notes: formData.notes || undefined
    };

    if (editingCustomer) {
      updateMutation.mutate({ id: editingCustomer.id, request });
    } else {
      createMutation.mutate(request);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">CRM</p>
          <h2 className="text-2xl font-semibold text-slate-900">Customers</h2>
          <p className="mt-1 text-sm text-slate-500">Manage guest information.</p>
        </div>
        <div className="flex items-center gap-3">
          <button
            onClick={openCreateModal}
            className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
          >
            Add customer
          </button>
        </div>
      </header>

      <section className="grid gap-4 md:grid-cols-3">
        {stats.map((stat) => (
          <div key={stat.label} className="panel px-4 py-5">
            <p className="text-xs uppercase tracking-[0.2em] text-slate-400">{stat.label}</p>
            <p className="mt-3 text-3xl font-semibold text-slate-900">{stat.value}</p>
          </div>
        ))}
      </section>

      <section className="panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-900">Customer list</h3>
          <div className="flex items-center gap-2">
            <input
              placeholder="Search by name, email, phone..."
              value={search}
              onChange={(e) => handleSearchChange(e.target.value)}
              className="w-64 rounded-full border border-slate-200 px-4 py-2 text-sm"
            />
          </div>
        </div>
        <div className="mt-4 overflow-hidden rounded-2xl border border-slate-200">
          <table className="w-full text-left text-sm">
            <thead className="bg-slate-50 text-xs uppercase tracking-[0.2em] text-slate-400">
              <tr>
                <th className="px-4 py-3">Name</th>
                <th className="px-4 py-3">Email</th>
                <th className="px-4 py-3">Phone</th>
                <th className="px-4 py-3">Created</th>
                <th className="px-4 py-3 text-right">Action</th>
              </tr>
            </thead>
            <tbody>
              {customersQuery.isLoading && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-slate-500">
                    Loading customers...
                  </td>
                </tr>
              )}
              {customersQuery.isError && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-rose-500">
                    Failed to load customers.
                  </td>
                </tr>
              )}
              {!customersQuery.isLoading && !customersQuery.isError && customers.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-4 py-6 text-center text-sm text-slate-500">
                    No customers found.
                  </td>
                </tr>
              )}
              {customers.map((customer: CustomerDto) => (
                <tr key={customer.id} className="border-t border-slate-100">
                  <td className="px-4 py-3 font-semibold text-slate-900">{customer.name}</td>
                  <td className="px-4 py-3 text-slate-600">{customer.email ?? "—"}</td>
                  <td className="px-4 py-3 text-slate-600">{customer.phone ?? "—"}</td>
                  <td className="px-4 py-3 text-slate-600">
                    {new Date(customer.createdAtUtc).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3 text-right">
                    <div className="flex justify-end gap-2">
                      <button
                        onClick={() => openEditModal(customer)}
                        className="rounded-full border border-slate-200 px-3 py-1 text-xs font-semibold uppercase text-slate-600 hover:bg-slate-50"
                      >
                        Edit
                      </button>
                      {isManager && (
                        <button
                          onClick={() => setDeleteConfirm(customer)}
                          className="rounded-full border border-rose-200 px-3 py-1 text-xs font-semibold uppercase text-rose-600 hover:bg-rose-50"
                        >
                          Delete
                        </button>
                      )}
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {/* Create/Edit Modal */}
      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">
              {editingCustomer ? "Edit Customer" : "Add Customer"}
            </h3>
            <form onSubmit={handleSubmit} className="mt-4 space-y-4">
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Name *
                </label>
                <input
                  type="text"
                  value={formData.name}
                  onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  required
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Email
                </label>
                <input
                  type="email"
                  value={formData.email}
                  onChange={(e) => setFormData({ ...formData, email: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Phone
                </label>
                <input
                  type="tel"
                  value={formData.phone}
                  onChange={(e) => setFormData({ ...formData, phone: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Address
                </label>
                <input
                  type="text"
                  value={formData.address}
                  onChange={(e) => setFormData({ ...formData, address: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                />
              </div>
              <div>
                <label className="block text-xs font-semibold uppercase tracking-[0.2em] text-slate-500">
                  Notes
                </label>
                <textarea
                  value={formData.notes}
                  onChange={(e) => setFormData({ ...formData, notes: e.target.value })}
                  className="mt-1 w-full rounded-xl border border-slate-200 px-4 py-2 text-sm"
                  rows={3}
                />
              </div>
              <div className="flex justify-end gap-3 pt-4">
                <button
                  type="button"
                  onClick={closeModal}
                  className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isPending}
                  className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
                >
                  {isPending ? "Saving..." : editingCustomer ? "Update" : "Create"}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      {/* Delete Confirmation Modal */}
      {deleteConfirm && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-sm rounded-3xl bg-white p-6 shadow-xl">
            <h3 className="text-lg font-semibold text-slate-900">Delete Customer</h3>
            <p className="mt-2 text-sm text-slate-600">
              Are you sure you want to delete{" "}
              <span className="font-semibold">{deleteConfirm.name}</span>? This action cannot be
              undone.
            </p>
            <div className="mt-6 flex justify-end gap-3">
              <button
                onClick={() => setDeleteConfirm(null)}
                className="rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteMutation.mutate(deleteConfirm.id)}
                disabled={deleteMutation.isPending}
                className="rounded-full bg-rose-600 px-5 py-2 text-xs font-semibold uppercase text-white disabled:opacity-50"
              >
                {deleteMutation.isPending ? "Deleting..." : "Delete"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
