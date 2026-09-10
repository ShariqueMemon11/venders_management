import React, { useCallback, useEffect, useRef, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { NotificationService, type AppNotification } from '../../services/notificationService';

const severityClass = (severity: string) => {
    switch (severity) {
        case 'Success': return 'text-success';
        case 'Warning': return 'text-warning';
        case 'Critical': return 'text-danger';
        default: return 'text-primary';
    }
};

const timeAgo = (iso: string) => {
    const diff = Date.now() - new Date(iso).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins} min ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
};

export const NotificationBell: React.FC = () => {
    const [open, setOpen] = useState(false);
    const [items, setItems] = useState<AppNotification[]>([]);
    const [count, setCount] = useState(0);
    const [loading, setLoading] = useState(false);
    const menuRef = useRef<HTMLDivElement>(null);
    const navigate = useNavigate();

    const refresh = useCallback(async () => {
        try {
            const [countRes, listRes] = await Promise.all([
                NotificationService.unreadCount(),
                NotificationService.list(false, 8)
            ]);
            setCount(countRes.data ?? 0);
            setItems(listRes.data ?? []);
        } catch {
            // ignore when logged out / API restarting
        }
    }, []);

    useEffect(() => {
        refresh();
        const id = window.setInterval(refresh, 15000);
        return () => window.clearInterval(id);
    }, [refresh]);

    useEffect(() => {
        const onClick = (e: MouseEvent) => {
            if (menuRef.current && !menuRef.current.contains(e.target as Node)) {
                setOpen(false);
            }
        };
        document.addEventListener('mousedown', onClick);
        return () => document.removeEventListener('mousedown', onClick);
    }, []);

    const onItemClick = async (n: AppNotification) => {
        if (!n.isRead) {
            await NotificationService.markRead(n.id);
            setCount((c) => Math.max(0, c - 1));
            setItems((prev) => prev.map((x) => x.id === n.id ? { ...x, isRead: true } : x));
        }
        setOpen(false);
        if (n.actionUrl) navigate(n.actionUrl);
    };

    const markAll = async () => {
        setLoading(true);
        try {
            await NotificationService.markAllRead();
            setCount(0);
            setItems((prev) => prev.map((x) => ({ ...x, isRead: true })));
        } finally {
            setLoading(false);
        }
    };

    return (
        <div className="position-relative" ref={menuRef}>
            <button
                className="btn btn-link text-body position-relative p-2"
                onClick={() => { setOpen((v) => !v); refresh(); }}
                title="Notifications"
            >
                <i className="bi bi-bell fs-5"></i>
                {count > 0 && (
                    <span className="position-absolute top-0 start-100 translate-middle badge rounded-pill bg-danger" style={{ fontSize: '0.65rem' }}>
                        {count > 99 ? '99+' : count}
                    </span>
                )}
            </button>

            {open && (
                <div className="dropdown-menu dropdown-menu-end show shadow border-0 p-0 notification-menu" style={{ width: 360, right: 0, left: 'auto', maxHeight: 420, overflow: 'hidden' }}>
                    <div className="d-flex justify-content-between align-items-center px-3 py-2 border-bottom bg-white">
                        <strong><i className="bi bi-bell me-2"></i>Notifications</strong>
                        <button className="btn btn-sm btn-link text-decoration-none" disabled={loading || count === 0} onClick={markAll}>
                            Mark all read
                        </button>
                    </div>
                    <div style={{ maxHeight: 300, overflowY: 'auto' }}>
                        {items.length === 0 ? (
                            <div className="text-muted text-center py-4 small">No notifications yet.</div>
                        ) : items.map((n) => (
                            <button
                                key={n.id}
                                className={`dropdown-item text-wrap py-3 border-bottom ${!n.isRead ? 'bg-primary bg-opacity-10' : ''}`}
                                onClick={() => onItemClick(n)}
                            >
                                <div className="d-flex gap-2">
                                    <span className={`mt-1 ${severityClass(n.severity)}`}>●</span>
                                    <div className="flex-grow-1">
                                        <div className="fw-semibold small">{n.title}</div>
                                        <div className="text-muted small">{n.message}</div>
                                        <div className="text-muted" style={{ fontSize: '0.7rem' }}>{timeAgo(n.createdAt)}</div>
                                    </div>
                                    {!n.isRead && <span className="badge bg-primary rounded-pill align-self-start">New</span>}
                                </div>
                            </button>
                        ))}
                    </div>
                    <div className="px-3 py-2 border-top bg-light text-center">
                        <Link to="/notifications" className="small fw-semibold text-decoration-none" onClick={() => setOpen(false)}>
                            View All
                        </Link>
                    </div>
                </div>
            )}
        </div>
    );
};
