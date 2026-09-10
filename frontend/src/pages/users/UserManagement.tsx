import React, { useCallback, useEffect, useState } from 'react';
import { getCurrentUser } from '../../auth/permissions';
import { FormErrorBanner } from '../../components/ui/FormErrorBanner';
import { EmptyState } from '../../components/ui/EmptyState';
import { StatusPill } from '../../components/ui/StatusPill';
import { useModal } from '../../components/ui/ModalContext';
import { UserService } from '../../services/userService';
import type { AppUserListItem, CreateUserRequest } from '../../types/index';
import { getApiErrorMessages } from '../../utils/apiErrors';

const ASSIGNABLE_ROLES: { value: CreateUserRequest['role']; label: string }[] = [
    { value: 'ProcurementManager', label: 'Procurement Manager' },
    { value: 'RiskAndCompliance', label: 'Risk & Compliance' },
    { value: 'Viewer', label: 'Viewer' },
];

const EMPTY_FORM: CreateUserRequest = {
    displayName: '',
    email: '',
    role: 'ProcurementManager',
    password: '',
};

export function roleLabel(role: string): string {
    return ASSIGNABLE_ROLES.find((r) => r.value === role)?.label
        ?? (role === 'Admin' ? 'Admin' : role);
}

export function userStatusForPill(user: AppUserListItem): string {
    if (user.isDeleted || user.status === 'Terminated') return 'Terminated';
    if (!user.isActive || user.status === 'Inactive') return 'Deactivated';
    return 'Active';
}

function isSameUser(rowEmail: string, currentEmail?: string): boolean {
    return !!currentEmail && rowEmail.trim().toLowerCase() === currentEmail.trim().toLowerCase();
}

export const UserManagement: React.FC = () => {
    const { showConfirm } = useModal();
    const currentUser = getCurrentUser();
    const [users, setUsers] = useState<AppUserListItem[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string[] | null>(null);
    const [formErrors, setFormErrors] = useState<string[] | null>(null);
    const [showCreate, setShowCreate] = useState(false);
    const [form, setForm] = useState<CreateUserRequest>(EMPTY_FORM);
    const [saving, setSaving] = useState(false);
    const [busyId, setBusyId] = useState<string | null>(null);

    const loadUsers = useCallback(async () => {
        setLoading(true);
        try {
            const response = await UserService.listUsers();
            if (response.success) {
                setUsers(response.data ?? []);
                setError(null);
            } else {
                setError([response.message || 'Users could not be loaded.']);
            }
        } catch (err: unknown) {
            setError(getApiErrorMessages(err));
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        void loadUsers();
    }, [loadUsers]);

    const openCreate = () => {
        setForm(EMPTY_FORM);
        setFormErrors(null);
        setShowCreate(true);
    };

    const closeCreate = () => {
        setShowCreate(false);
        setFormErrors(null);
        setForm(EMPTY_FORM);
    };

    const handleCreate = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!form.displayName.trim()) {
            setFormErrors(['Display name is required.']);
            return;
        }
        if (!form.email.trim()) {
            setFormErrors(['Email is required.']);
            return;
        }
        if (!form.password) {
            setFormErrors(['Password is required.']);
            return;
        }
        if (form.password.length < 8) {
            setFormErrors(['Password must be at least 8 characters.']);
            return;
        }

        setSaving(true);
        setFormErrors(null);
        try {
            const response = await UserService.createUser({
                ...form,
                displayName: form.displayName.trim(),
                email: form.email.trim(),
            });
            if (response.success) {
                closeCreate();
                await loadUsers();
            } else {
                setFormErrors([response.message || 'User was not created.']);
            }
        } catch (err: unknown) {
            setFormErrors(getApiErrorMessages(err));
        } finally {
            setSaving(false);
        }
    };

    const handleToggleActive = async (user: AppUserListItem) => {
        setBusyId(user.id);
        setError(null);
        try {
            const response = user.isActive
                ? await UserService.deactivateUser(user.id)
                : await UserService.activateUser(user.id);
            if (response.success) {
                await loadUsers();
            } else {
                setError([response.message || 'User status was not updated.']);
            }
        } catch (err: unknown) {
            setError(getApiErrorMessages(err));
        } finally {
            setBusyId(null);
        }
    };

    const handleTerminate = async (user: AppUserListItem) => {
        const confirmed = await showConfirm(
            `This is permanent — ${user.displayName} will never be able to log in again. Continue?`,
            'Terminate user'
        );
        if (!confirmed) return;

        setBusyId(user.id);
        setError(null);
        try {
            const response = await UserService.terminateUser(user.id);
            if (response.success) {
                await loadUsers();
            } else {
                setError([response.message || 'User was not terminated.']);
            }
        } catch (err: unknown) {
            setError(getApiErrorMessages(err));
        } finally {
            setBusyId(null);
        }
    };

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Administration</div>
                    <h2 className="fw-bold mb-0">Manage Users</h2>
                </div>
                <button type="button" className="btn btn-primary shadow-sm" onClick={openCreate}>
                    <i className="bi bi-plus-lg me-2"></i> Create User
                </button>
            </div>

            <FormErrorBanner messages={error} />

            <div className="card shadow-sm border-0">
                <div className="card-body p-0">
                    <div className="table-responsive">
                        <table className="table table-hover align-middle mb-0">
                            <thead className="bg-light">
                                <tr>
                                    <th className="ps-4">Name</th>
                                    <th>Email</th>
                                    <th>Role</th>
                                    <th>Status</th>
                                    <th className="text-end pe-4">Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {loading ? (
                                    <tr>
                                        <td colSpan={5} className="text-center py-5">
                                            <div className="spinner-border text-primary"></div>
                                        </td>
                                    </tr>
                                ) : users.length === 0 ? (
                                    <tr>
                                        <td colSpan={5}>
                                            <EmptyState message="No users to show." />
                                        </td>
                                    </tr>
                                ) : (
                                    users.map((u) => {
                                        const self = isSameUser(u.email, currentUser?.email);
                                        const terminated = u.isDeleted || u.status === 'Terminated';
                                        const pillStatus = userStatusForPill(u);
                                        return (
                                            <tr key={u.id} data-user-email={u.email}>
                                                <td className="ps-4 fw-bold text-dark">{u.displayName}</td>
                                                <td>{u.email}</td>
                                                <td>{roleLabel(u.role)}</td>
                                                <td><StatusPill status={pillStatus} /></td>
                                                <td className="text-end pe-4">
                                                    {self || terminated ? (
                                                        <span className="text-muted small">—</span>
                                                    ) : (
                                                        <div className="d-inline-flex gap-2">
                                                            <button
                                                                type="button"
                                                                className="btn btn-sm btn-outline-secondary"
                                                                disabled={busyId === u.id}
                                                                onClick={() => void handleToggleActive(u)}
                                                            >
                                                                {u.isActive ? 'Deactivate' : 'Activate'}
                                                            </button>
                                                            <button
                                                                type="button"
                                                                className="btn btn-sm btn-outline-danger"
                                                                disabled={busyId === u.id}
                                                                onClick={() => void handleTerminate(u)}
                                                            >
                                                                Terminate
                                                            </button>
                                                        </div>
                                                    )}
                                                </td>
                                            </tr>
                                        );
                                    })
                                )}
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>

            {showCreate && (
                <div className="modal show d-block" tabIndex={-1} style={{ backgroundColor: 'rgba(0,0,0,0.5)' }}>
                    <div className="modal-dialog">
                        <div className="modal-content">
                            <form onSubmit={handleCreate}>
                                <div className="modal-header">
                                    <h5 className="modal-title">Create User</h5>
                                    <button type="button" className="btn-close" onClick={closeCreate}></button>
                                </div>
                                <div className="modal-body">
                                    <FormErrorBanner messages={formErrors} />
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="user-display-name">Display name *</label>
                                        <input
                                            id="user-display-name"
                                            type="text"
                                            className="form-control"
                                            required
                                            value={form.displayName}
                                            onChange={(e) => setForm({ ...form, displayName: e.target.value })}
                                        />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="user-email">Email *</label>
                                        <input
                                            id="user-email"
                                            type="email"
                                            className="form-control"
                                            required
                                            value={form.email}
                                            onChange={(e) => setForm({ ...form, email: e.target.value })}
                                        />
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="user-role">Role *</label>
                                        <select
                                            id="user-role"
                                            className="form-select"
                                            value={form.role}
                                            onChange={(e) => setForm({ ...form, role: e.target.value })}
                                        >
                                            {ASSIGNABLE_ROLES.map((r) => (
                                                <option key={r.value} value={r.value}>{r.label}</option>
                                            ))}
                                        </select>
                                    </div>
                                    <div className="mb-3">
                                        <label className="form-label" htmlFor="user-password">Password *</label>
                                        <input
                                            id="user-password"
                                            type="password"
                                            className="form-control"
                                            required
                                            minLength={8}
                                            value={form.password}
                                            onChange={(e) => setForm({ ...form, password: e.target.value })}
                                        />
                                        <div className="form-text">At least 8 characters.</div>
                                    </div>
                                </div>
                                <div className="modal-footer">
                                    <button type="button" className="btn btn-secondary" onClick={closeCreate}>Cancel</button>
                                    <button type="submit" className="btn btn-primary" disabled={saving}>
                                        {saving ? 'Creating...' : 'Create User'}
                                    </button>
                                </div>
                            </form>
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
};
