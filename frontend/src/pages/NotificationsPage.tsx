import React, { useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { NotificationService, type AppNotification } from '../services/notificationService';
import { EmptyState } from '../components/ui/EmptyState';

type Filter = 'all' | 'unread' | 'approvals' | 'system';

export const NotificationsPage: React.FC = () => {
    const [items, setItems] = useState<AppNotification[]>([]);
    const [filter, setFilter] = useState<Filter>('all');
    const [loading, setLoading] = useState(true);

    const load = async () => {
        setLoading(true);
        try {
            const res = await NotificationService.list(filter === 'unread', 100);
            setItems(res.data ?? []);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => { load(); }, [filter]);

    const visible = useMemo(() => {
        if (filter === 'approvals') {
            return items.filter((n) =>
                ['VendorSubmitted', 'WorkflowAssigned', 'VendorApproved', 'VendorRejected', 'WorkflowCompleted'].includes(n.type));
        }
        if (filter === 'system') {
            return items.filter((n) => n.type === 'System');
        }
        return items;
    }, [items, filter]);

    const markAll = async () => {
        await NotificationService.markAllRead();
        await load();
    };

    const onOpen = async (n: AppNotification) => {
        if (!n.isRead) await NotificationService.markRead(n.id);
    };

    const onDelete = async (id: string) => {
        await NotificationService.remove(id);
        setItems((prev) => prev.filter((x) => x.id !== id));
    };

    return (
        <div className="container-fluid mb-5">
            <div className="d-flex justify-content-between align-items-center mb-4">
                <div>
                    <div className="text-muted small">Inbox</div>
                    <h2 className="fw-bold mb-0">Notification Center</h2>
                </div>
                <button className="btn btn-outline-primary" onClick={markAll}>Mark all as read</button>
            </div>

            <ul className="nav nav-pills mb-3 gap-2">
                {([
                    ['all', 'All'],
                    ['unread', 'Unread'],
                    ['approvals', 'Approvals'],
                    ['system', 'System'],
                ] as [Filter, string][]).map(([key, label]) => (
                    <li className="nav-item" key={key}>
                        <button className={`nav-link ${filter === key ? 'active' : ''}`} onClick={() => setFilter(key)}>
                            {label}
                        </button>
                    </li>
                ))}
            </ul>

            <div className="card shadow-sm border-0">
                <div className="card-body p-0">
                    {loading ? (
                        <div className="text-center py-5"><div className="spinner-border text-primary"></div></div>
                    ) : visible.length === 0 ? (
                        <EmptyState message="You're all caught up." />
                    ) : (
                        <div className="list-group list-group-flush">
                            {visible.map((n) => (
                                <div key={n.id} className={`list-group-item px-4 py-3 ${!n.isRead ? 'bg-primary bg-opacity-10' : ''}`}>
                                    <div className="d-flex justify-content-between gap-3">
                                        <div>
                                            <div className="fw-semibold">
                                                {!n.isRead && <span className="badge bg-primary me-2">Unread</span>}
                                                <span className="badge bg-secondary me-2">{n.type}</span>
                                                {n.title}
                                            </div>
                                            <div className="text-muted mt-1">{n.message}</div>
                                            <div className="small text-muted mt-1">{new Date(n.createdAt).toLocaleString()}</div>
                                        </div>
                                        <div className="d-flex flex-column gap-2 align-items-end">
                                            {n.actionUrl && (
                                                <Link className="btn btn-sm btn-outline-primary" to={n.actionUrl} onClick={() => onOpen(n)}>
                                                    Open
                                                </Link>
                                            )}
                                            {!n.isRead && (
                                                <button className="btn btn-sm btn-link" onClick={async () => { await NotificationService.markRead(n.id); await load(); }}>
                                                    Mark read
                                                </button>
                                            )}
                                            <button className="btn btn-sm btn-link text-danger" onClick={() => onDelete(n.id)}>
                                                Remove
                                            </button>
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};
