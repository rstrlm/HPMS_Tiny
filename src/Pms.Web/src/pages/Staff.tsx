import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { getStaff, createStaff, updateStaff, deleteStaff, createStaffWithKeycloak } from "../api/staff";
import type {
  StaffProfileDto,
  CreateStaffProfileRequest,
  UpdateStaffProfileRequest,
  CreateStaffWithKeycloakRequest
} from "../api/types";

const SKILL_OPTIONS = [
  { value: "frontdesk", label: "Front Desk" },
  { value: "therapist", label: "Therapist" },
  { value: "cleaner", label: "Cleaner" },
  { value: "maintenance", label: "Maintenance" },
  { value: "accounting", label: "Accounting" }
];

const ROLE_OPTIONS = [
  { value: "manager", label: "Manager" },
  { value: "frontdesk", label: "Front Desk" },
  { value: "therapist", label: "Therapist" },
  { value: "cleaner", label: "Cleaner" },
  { value: "maintenance", label: "Maintenance" },
  { value: "accounting", label: "Accounting" }
];

const formatDate = (dateStr: string) => {
  return new Date(dateStr).toLocaleDateString("fi-FI");
};

const parseSkills = (skills: string | null | undefined): string[] => {
  if (!skills) return [];
  return skills.split(",").map((s) => s.trim().toLowerCase()).filter(Boolean);
};

const formatSkills = (skills: string[]): string => {
  return skills.join(", ");
};

export default function Staff() {
  const queryClient = useQueryClient();

  const [showInactive, setShowInactive] = useState(false);
  const [searchQuery, setSearchQuery] = useState("");
  const [showAddModal, setShowAddModal] = useState(false);
  const [editingStaff, setEditingStaff] = useState<StaffProfileDto | null>(null);

  // Add form mode: "link" = link existing Keycloak user, "create" = create new user in Keycloak
  const [addMode, setAddMode] = useState<"link" | "create">("create");

  // Add form state (link existing user)
  const [formKeycloakUserId, setFormKeycloakUserId] = useState("");
  const [formDisplayName, setFormDisplayName] = useState("");
  const [formEmail, setFormEmail] = useState("");
  const [formSkills, setFormSkills] = useState<string[]>([]);

  // Add form state (create new user in Keycloak)
  const [formUsername, setFormUsername] = useState("");
  const [formPassword, setFormPassword] = useState("");
  const [formRoles, setFormRoles] = useState<string[]>([]);

  // Edit form state
  const [editDisplayName, setEditDisplayName] = useState("");
  const [editEmail, setEditEmail] = useState("");
  const [editSkills, setEditSkills] = useState<string[]>([]);
  const [editIsActive, setEditIsActive] = useState(true);

  const staffQuery = useQuery({
    queryKey: ["staff", { activeOnly: showInactive ? undefined : true, search: searchQuery || undefined }],
    queryFn: () => getStaff(showInactive ? undefined : true, searchQuery || undefined)
  });

  const createMutation = useMutation({
    mutationFn: (request: CreateStaffProfileRequest) => createStaff(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["staff"] });
      resetAddForm();
      setShowAddModal(false);
    }
  });

  const createWithKeycloakMutation = useMutation({
    mutationFn: (request: CreateStaffWithKeycloakRequest) => createStaffWithKeycloak(request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["staff"] });
      resetAddForm();
      setShowAddModal(false);
    }
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, request }: { id: string; request: UpdateStaffProfileRequest }) =>
      updateStaff(id, request),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["staff"] });
      setEditingStaff(null);
    }
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => deleteStaff(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["staff"] });
    }
  });

  const resetAddForm = () => {
    setFormKeycloakUserId("");
    setFormDisplayName("");
    setFormEmail("");
    setFormSkills([]);
    setFormUsername("");
    setFormPassword("");
    setFormRoles([]);
    setAddMode("create");
  };

  const handleCreate = () => {
    if (addMode === "link") {
      if (!formKeycloakUserId || !formDisplayName) return;

      createMutation.mutate({
        keycloakUserId: formKeycloakUserId,
        displayName: formDisplayName,
        email: formEmail || undefined,
        skills: formatSkills(formSkills) || undefined
      });
    } else {
      if (!formUsername || !formPassword || !formDisplayName || !formEmail) return;

      createWithKeycloakMutation.mutate({
        username: formUsername,
        password: formPassword,
        displayName: formDisplayName,
        email: formEmail,
        skills: formatSkills(formSkills) || undefined,
        roles: formRoles.length > 0 ? formRoles : undefined
      });
    }
  };

  const isCreating = createMutation.isPending || createWithKeycloakMutation.isPending;
  const createError = createMutation.isError || createWithKeycloakMutation.isError;

  const handleEdit = (staff: StaffProfileDto) => {
    setEditingStaff(staff);
    setEditDisplayName(staff.displayName);
    setEditEmail(staff.email ?? "");
    setEditSkills(parseSkills(staff.skills));
    setEditIsActive(staff.isActive);
  };

  const handleUpdate = () => {
    if (!editingStaff) return;

    updateMutation.mutate({
      id: editingStaff.id,
      request: {
        displayName: editDisplayName || undefined,
        email: editEmail || undefined,
        skills: formatSkills(editSkills) || undefined,
        isActive: editIsActive
      }
    });
  };

  const handleDeactivate = (staff: StaffProfileDto) => {
    if (confirm(`Are you sure you want to deactivate ${staff.displayName}?`)) {
      deleteMutation.mutate(staff.id);
    }
  };

  const toggleSkill = (skill: string, skills: string[], setSkills: (skills: string[]) => void) => {
    if (skills.includes(skill)) {
      setSkills(skills.filter((s) => s !== skill));
    } else {
      setSkills([...skills, skill]);
    }
  };

  const staffList = staffQuery.data ?? [];

  return (
    <div className="space-y-6">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div>
          <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Administration</p>
          <h2 className="text-2xl font-semibold text-slate-900">Staff Management</h2>
          <p className="mt-1 text-sm text-slate-500">Manage staff members and their roles.</p>
        </div>
        <button
          onClick={() => setShowAddModal(true)}
          className="rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white"
        >
          Add Staff
        </button>
      </header>

      {/* Stats */}
      <section className="grid gap-4 md:grid-cols-4">
        <div className="panel px-4 py-5">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Total Staff</p>
          <p className="mt-3 text-3xl font-semibold text-slate-900">{staffList.length}</p>
        </div>
        <div className="panel px-4 py-5">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Active</p>
          <p className="mt-3 text-3xl font-semibold text-emerald-600">
            {staffList.filter((s) => s.isActive).length}
          </p>
        </div>
        <div className="panel px-4 py-5">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Therapists</p>
          <p className="mt-3 text-3xl font-semibold text-slate-900">
            {staffList.filter((s) => s.skills?.toLowerCase().includes("therapist")).length}
          </p>
        </div>
        <div className="panel px-4 py-5">
          <p className="text-xs uppercase tracking-[0.2em] text-slate-400">Cleaners</p>
          <p className="mt-3 text-3xl font-semibold text-slate-900">
            {staffList.filter((s) => s.skills?.toLowerCase().includes("cleaner")).length}
          </p>
        </div>
      </section>

      {/* Filters */}
      <section className="panel p-6">
        <div className="flex flex-wrap items-center justify-between gap-4">
          <h3 className="text-lg font-semibold text-slate-900">Staff List</h3>
          <div className="flex items-center gap-4">
            <input
              type="text"
              placeholder="Search by name..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="rounded-full border border-slate-200 px-4 py-2 text-sm"
            />
            <label className="flex items-center gap-2 text-sm text-slate-600">
              <input
                type="checkbox"
                checked={showInactive}
                onChange={(e) => setShowInactive(e.target.checked)}
                className="h-4 w-4 rounded border-slate-300"
              />
              Show inactive
            </label>
          </div>
        </div>

        <div className="mt-4 overflow-hidden rounded-xl border border-slate-200">
          {staffQuery.isLoading && (
            <div className="px-4 py-6 text-center text-sm text-slate-500">Loading staff...</div>
          )}
          {staffQuery.isError && (
            <div className="px-4 py-6 text-center text-sm text-rose-600">Failed to load staff</div>
          )}
          {!staffQuery.isLoading && staffList.length === 0 && (
            <div className="px-4 py-6 text-center text-sm text-slate-500">
              No staff members found.{" "}
              <button onClick={() => setShowAddModal(true)} className="text-slate-900 underline">
                Add one
              </button>
            </div>
          )}
          {staffList.length > 0 && (
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-400">
                <tr>
                  <th className="px-4 py-3">Name</th>
                  <th className="px-4 py-3">Email</th>
                  <th className="px-4 py-3">Skills/Roles</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Created</th>
                  <th className="px-4 py-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody>
                {staffList.map((staff) => (
                  <tr key={staff.id} className="border-t border-slate-100">
                    <td className="px-4 py-3 font-medium text-slate-900">{staff.displayName}</td>
                    <td className="px-4 py-3 text-slate-600">{staff.email ?? "—"}</td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {parseSkills(staff.skills).map((skill) => (
                          <span
                            key={skill}
                            className="rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600"
                          >
                            {skill}
                          </span>
                        ))}
                        {!staff.skills && <span className="text-slate-400">—</span>}
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span
                        className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                          staff.isActive
                            ? "bg-emerald-100 text-emerald-700"
                            : "bg-slate-100 text-slate-500"
                        }`}
                      >
                        {staff.isActive ? "Active" : "Inactive"}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-slate-600">{formatDate(staff.createdAtUtc)}</td>
                    <td className="px-4 py-3 text-right">
                      <button
                        onClick={() => handleEdit(staff)}
                        className="mr-2 text-slate-600 hover:text-slate-900"
                      >
                        Edit
                      </button>
                      {staff.isActive && (
                        <button
                          onClick={() => handleDeactivate(staff)}
                          disabled={deleteMutation.isPending}
                          className="text-rose-600 hover:text-rose-700 disabled:opacity-50"
                        >
                          Deactivate
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      {/* Add Staff Modal */}
      {showAddModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl max-h-[90vh] overflow-y-auto">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">New</p>
              <h3 className="text-xl font-semibold text-slate-900">Add Staff Member</h3>
            </div>

            {/* Mode selector */}
            <div className="mb-6 flex rounded-xl border border-slate-200 p-1">
              <button
                type="button"
                onClick={() => setAddMode("create")}
                className={`flex-1 rounded-lg px-4 py-2 text-xs font-semibold uppercase tracking-[0.1em] transition ${
                  addMode === "create"
                    ? "bg-slate-900 text-white"
                    : "text-slate-600 hover:bg-slate-50"
                }`}
              >
                Create New User
              </button>
              <button
                type="button"
                onClick={() => setAddMode("link")}
                className={`flex-1 rounded-lg px-4 py-2 text-xs font-semibold uppercase tracking-[0.1em] transition ${
                  addMode === "link"
                    ? "bg-slate-900 text-white"
                    : "text-slate-600 hover:bg-slate-50"
                }`}
              >
                Link Existing
              </button>
            </div>

            <div className="space-y-4">
              {addMode === "create" ? (
                <>
                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Username *
                    </label>
                    <input
                      type="text"
                      value={formUsername}
                      onChange={(e) => setFormUsername(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="jsmith"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Password *
                    </label>
                    <input
                      type="password"
                      value={formPassword}
                      onChange={(e) => setFormPassword(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="Temporary password"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Display Name *
                    </label>
                    <input
                      type="text"
                      value={formDisplayName}
                      onChange={(e) => setFormDisplayName(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="John Smith"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Email *
                    </label>
                    <input
                      type="email"
                      value={formEmail}
                      onChange={(e) => setFormEmail(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="john@example.com"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Keycloak Roles
                    </label>
                    <div className="flex flex-wrap gap-2">
                      {ROLE_OPTIONS.map((opt) => (
                        <button
                          key={opt.value}
                          type="button"
                          onClick={() => toggleSkill(opt.value, formRoles, setFormRoles)}
                          className={`rounded-full border px-3 py-1 text-xs font-medium transition ${
                            formRoles.includes(opt.value)
                              ? "border-indigo-600 bg-indigo-600 text-white"
                              : "border-slate-200 text-slate-600 hover:bg-slate-50"
                          }`}
                        >
                          {opt.label}
                        </button>
                      ))}
                    </div>
                    <p className="mt-1 text-xs text-slate-400">
                      Roles for login access and permissions
                    </p>
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Skills (operational)
                    </label>
                    <div className="flex flex-wrap gap-2">
                      {SKILL_OPTIONS.map((opt) => (
                        <button
                          key={opt.value}
                          type="button"
                          onClick={() => toggleSkill(opt.value, formSkills, setFormSkills)}
                          className={`rounded-full border px-3 py-1 text-xs font-medium transition ${
                            formSkills.includes(opt.value)
                              ? "border-slate-900 bg-slate-900 text-white"
                              : "border-slate-200 text-slate-600 hover:bg-slate-50"
                          }`}
                        >
                          {opt.label}
                        </button>
                      ))}
                    </div>
                    <p className="mt-1 text-xs text-slate-400">
                      Skills for task assignment (e.g., cleaning, therapies)
                    </p>
                  </div>
                </>
              ) : (
                <>
                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Keycloak User ID *
                    </label>
                    <input
                      type="text"
                      value={formKeycloakUserId}
                      onChange={(e) => setFormKeycloakUserId(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="UUID from Keycloak"
                    />
                    <p className="mt-1 text-xs text-slate-400">
                      The user must already exist in Keycloak
                    </p>
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Display Name *
                    </label>
                    <input
                      type="text"
                      value={formDisplayName}
                      onChange={(e) => setFormDisplayName(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="John Smith"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Email
                    </label>
                    <input
                      type="email"
                      value={formEmail}
                      onChange={(e) => setFormEmail(e.target.value)}
                      className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                      placeholder="john@example.com"
                    />
                  </div>

                  <div>
                    <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                      Skills
                    </label>
                    <div className="flex flex-wrap gap-2">
                      {SKILL_OPTIONS.map((opt) => (
                        <button
                          key={opt.value}
                          type="button"
                          onClick={() => toggleSkill(opt.value, formSkills, setFormSkills)}
                          className={`rounded-full border px-3 py-1 text-xs font-medium transition ${
                            formSkills.includes(opt.value)
                              ? "border-slate-900 bg-slate-900 text-white"
                              : "border-slate-200 text-slate-600 hover:bg-slate-50"
                          }`}
                        >
                          {opt.label}
                        </button>
                      ))}
                    </div>
                  </div>
                </>
              )}
            </div>

            {createError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to create staff member.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => {
                  resetAddForm();
                  setShowAddModal(false);
                }}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleCreate}
                disabled={
                  isCreating ||
                  (addMode === "link" && (!formKeycloakUserId || !formDisplayName)) ||
                  (addMode === "create" && (!formUsername || !formPassword || !formDisplayName || !formEmail))
                }
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {isCreating ? "Creating..." : "Create"}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Edit Staff Modal */}
      {editingStaff && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="w-full max-w-md rounded-2xl bg-white p-6 shadow-xl">
            <div className="mb-6">
              <p className="text-xs uppercase tracking-[0.3em] text-slate-400">Edit</p>
              <h3 className="text-xl font-semibold text-slate-900">{editingStaff.displayName}</h3>
            </div>

            <div className="space-y-4">
              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Display Name
                </label>
                <input
                  type="text"
                  value={editDisplayName}
                  onChange={(e) => setEditDisplayName(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Email
                </label>
                <input
                  type="email"
                  value={editEmail}
                  onChange={(e) => setEditEmail(e.target.value)}
                  className="w-full rounded-xl border border-slate-200 px-4 py-3 text-sm"
                />
              </div>

              <div>
                <label className="mb-1 block text-xs uppercase tracking-[0.2em] text-slate-500">
                  Skills/Roles
                </label>
                <div className="flex flex-wrap gap-2">
                  {SKILL_OPTIONS.map((opt) => (
                    <button
                      key={opt.value}
                      type="button"
                      onClick={() => toggleSkill(opt.value, editSkills, setEditSkills)}
                      className={`rounded-full border px-3 py-1 text-xs font-medium transition ${
                        editSkills.includes(opt.value)
                          ? "border-slate-900 bg-slate-900 text-white"
                          : "border-slate-200 text-slate-600 hover:bg-slate-50"
                      }`}
                    >
                      {opt.label}
                    </button>
                  ))}
                </div>
              </div>

              <div className="flex items-center gap-2">
                <input
                  type="checkbox"
                  id="editIsActive"
                  checked={editIsActive}
                  onChange={(e) => setEditIsActive(e.target.checked)}
                  className="h-4 w-4 rounded border-slate-300"
                />
                <label htmlFor="editIsActive" className="text-sm text-slate-600">
                  Active
                </label>
              </div>
            </div>

            {updateMutation.isError && (
              <div className="mt-4 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3">
                <p className="text-sm text-rose-700">Failed to update staff member.</p>
              </div>
            )}

            <div className="mt-6 flex gap-3">
              <button
                onClick={() => setEditingStaff(null)}
                className="flex-1 rounded-full border border-slate-200 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-slate-600"
              >
                Cancel
              </button>
              <button
                onClick={handleUpdate}
                disabled={updateMutation.isPending}
                className="flex-1 rounded-full bg-slate-900 px-5 py-2 text-xs font-semibold uppercase tracking-[0.2em] text-white disabled:opacity-50"
              >
                {updateMutation.isPending ? "Saving..." : "Save"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
