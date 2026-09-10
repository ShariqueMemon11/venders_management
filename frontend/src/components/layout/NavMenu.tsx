import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';

const NavMenu: React.FC = () => {
    const navigate = useNavigate();

    const handleSignOut = () => {
        sessionStorage.removeItem('auth_token');
        sessionStorage.removeItem('user');
        navigate('/login');
    };

    const userStr = sessionStorage.getItem('user');
    const user = userStr ? JSON.parse(userStr) : null;
    const isProcurement = user?.role === 'Admin' || user?.role === 'ProcurementManager';
    const isAdmin = user?.role === 'Admin';
    const canReports = user?.role === 'Admin' || user?.role === 'ProcurementManager' || user?.role === 'RiskAndCompliance';

    return (
        <div className="d-flex flex-column h-100">
            <div className="p-3 border-bottom border-secondary mb-2">
                <h5 className="mb-0 text-white"><i className="bi bi-box-seam me-2"></i>Vendor Platform</h5>
            </div>
            
            <nav className="nav flex-column">
                <NavLink to="/" className={({isActive}) => `nav-link ${isActive ? 'active' : ''}`} end>
                    <i className="bi bi-house-door me-3"></i>Dashboard
                </NavLink>
                {isProcurement && (
                    <NavLink to="/vendors" className={({isActive}) => `nav-link ${isActive && !window.location.pathname.includes('/directory') ? 'active' : ''}`} end>
                        <i className="bi bi-people me-3"></i>Suppliers
                    </NavLink>
                )}
                <NavLink to="/vendors/directory" className={({isActive}) => `nav-link ${isActive ? 'active' : ''}`}>
                    <i className="bi bi-journal-richtext me-3"></i>Vendor Details
                </NavLink>
                <NavLink to="/notifications" className={({isActive}) => `nav-link ${isActive ? 'active' : ''}`}>
                    <i className="bi bi-bell me-3"></i>Notifications
                </NavLink>
                {canReports && (
                    <NavLink to="/reports" className={({isActive}) => `nav-link ${isActive ? 'active' : ''}`}>
                        <i className="bi bi-bar-chart me-3"></i>Reports
                    </NavLink>
                )}
                {isAdmin && (
                    <NavLink to="/users" className={({isActive}) => `nav-link ${isActive ? 'active' : ''}`}>
                        <i className="bi bi-person-gear me-3"></i>Users
                    </NavLink>
                )}
            </nav>
            
            <div className="mt-auto p-3">
                {user && (
                    <div className="mb-2 text-center text-light">
                        <div className="small fw-bold">{user.name}</div>
                        <div className="small text-muted" style={{fontSize: '0.75rem'}}>{user.role}</div>
                    </div>
                )}
                <button className="btn btn-outline-secondary w-100 btn-sm text-light border-secondary" onClick={handleSignOut}>
                    <i className="bi bi-box-arrow-right me-2"></i>Sign Out
                </button>
            </div>
        </div>
    );
};

export default NavMenu;