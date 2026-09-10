import React from 'react';
import { Routes, Route, Navigate, useLocation } from 'react-router-dom';
import MainLayout from './components/layout/MainLayout';
import { Login } from './pages/Login';
import { Dashboard } from './pages/Dashboard';
import { VendorList } from './pages/vendors/VendorList';
import { VendorDetailsView } from './pages/vendors/VendorDetails';
import { VendorCreate } from './pages/vendors/VendorCreate';
import { VendorEdit } from './pages/vendors/VendorEdit';
import { Reports } from './pages/Reports';
import { NotificationsPage } from './pages/NotificationsPage';
import { UserManagement } from './pages/users/UserManagement';
import { ModalProvider } from './components/ui/ModalContext';

import { VendorDirectory } from './pages/vendors/VendorDirectory';

// Simple Auth Guard
const RequireAuth = ({ children, allowedRoles }: { children: React.ReactElement, allowedRoles?: string[] }) => {
    const token = sessionStorage.getItem('auth_token');
    const location = useLocation();
    
    if (!token) return <Navigate to="/login" state={{ from: location }} replace />;
    
    if (allowedRoles && allowedRoles.length > 0) {
        const userStr = sessionStorage.getItem('user');
        const user = userStr ? JSON.parse(userStr) : null;
        if (!user || !allowedRoles.includes(user.role)) {
            return <Navigate to="/" replace />; // Or to a 'Not Authorized' page
        }
    }
    
    return children;
};

const App: React.FC = () => {
  return (
    <ModalProvider>
        <Routes>
          <Route path="/login" element={<Login />} />
          
          <Route path="/" element={<RequireAuth><MainLayout /></RequireAuth>}>
            <Route index element={<Dashboard />} />
            
            <Route path="vendors">
                <Route index element={<RequireAuth allowedRoles={['Admin', 'ProcurementManager']}><VendorList /></RequireAuth>} />
                <Route path="directory" element={<VendorDirectory />} />
                <Route path="new" element={<RequireAuth allowedRoles={['Admin', 'ProcurementManager']}><VendorCreate /></RequireAuth>} />
                <Route path=":id" element={<VendorDetailsView />} />
                <Route path=":id/edit" element={<RequireAuth allowedRoles={['Admin', 'ProcurementManager']}><VendorEdit /></RequireAuth>} />
            </Route>
            
            <Route path="notifications" element={<NotificationsPage />} />
            <Route path="reports" element={<RequireAuth allowedRoles={['Admin', 'ProcurementManager', 'RiskAndCompliance']}><Reports /></RequireAuth>} />
            <Route path="users" element={<RequireAuth allowedRoles={['Admin']}><UserManagement /></RequireAuth>} />
          </Route>
        </Routes>
    </ModalProvider>
  );
};

export default App;