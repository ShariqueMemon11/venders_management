import { render } from '@testing-library/react';
import type { ReactElement } from 'react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { ModalProvider } from '../components/ui/ModalContext';

export function asProcurementUser() {
  sessionStorage.setItem(
    'user',
    JSON.stringify({
      email: 'procurement@example.com',
      name: 'Procurement User',
      role: 'ProcurementManager',
      permissions: ['Vendor.Create', 'Vendor.Edit'],
    })
  );
}

export function asAdminUser() {
  sessionStorage.setItem(
    'user',
    JSON.stringify({
      email: 'admin@example.com',
      name: 'System Admin',
      role: 'Admin',
      permissions: ['User.Manage'],
    })
  );
}

export function renderWithRouter(
  ui: ReactElement,
  { route = '/', path = '*' }: { route?: string; path?: string } = {}
) {
  return render(
    <MemoryRouter initialEntries={[route]}>
      <ModalProvider>
        <Routes>
          <Route path={path} element={ui} />
          <Route path="/vendors/:id" element={<div data-testid="vendor-detail">Vendor Detail</div>} />
          <Route path="/vendors/directory" element={<div data-testid="directory">Directory</div>} />
          <Route path="/vendors" element={<div data-testid="vendors-list">Vendors</div>} />
        </Routes>
      </ModalProvider>
    </MemoryRouter>
  );
}
