import React from 'react';
import { Outlet } from 'react-router-dom';
import NavMenu from './NavMenu';
import { NotificationBell } from '../notifications/NotificationBell';
import { ThemeToggle } from '../ui/ThemeToggle';
import { getCurrentUser } from '../../auth/permissions';

const MainLayout: React.FC = () => {
    const user = getCurrentUser();

    return (
        <div className="d-flex">
            <aside className="sidebar shadow-sm">
                <NavMenu />
            </aside>
            <main className="main-content flex-grow-1 bg-light">
                <div className="page-topbar bg-white border-bottom d-flex justify-content-between align-items-center sticky-top">
                    <h5 className="mb-0 text-dark fw-bold">Admin Portal</h5>
                    <div className="d-flex align-items-center gap-2">
                        <ThemeToggle />
                        <NotificationBell />
                        <span className="text-muted small">
                            <span className="user-chip-prefix">Logged in as: </span>
                            <strong>{user?.name || user?.role || 'User'}</strong>
                        </span>
                    </div>
                </div>
                <div className="page-body">
                    <Outlet />
                </div>
            </main>
        </div>
    );
};

export default MainLayout;
