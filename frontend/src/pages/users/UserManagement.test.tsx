import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { UserManagement, userStatusForPill, roleLabel } from './UserManagement';
import { asAdminUser, renderWithRouter } from '../../test/test-utils';
import { UserService } from '../../services/userService';
import type { AppUserListItem } from '../../types/index';

vi.mock('../../services/userService', () => ({
  UserService: {
    listUsers: vi.fn(),
    createUser: vi.fn(),
    deactivateUser: vi.fn(),
    activateUser: vi.fn(),
    terminateUser: vi.fn(),
  },
}));

const admin: AppUserListItem = {
  id: 'admin-id',
  displayName: 'System Admin',
  email: 'admin@example.com',
  role: 'Admin',
  isActive: true,
  isDeleted: false,
  status: 'Active',
  createdAt: '2026-01-01T00:00:00Z',
};

const procurement: AppUserListItem = {
  id: 'proc-id',
  displayName: 'Pat Procure',
  email: 'pat@example.com',
  role: 'ProcurementManager',
  isActive: true,
  isDeleted: false,
  status: 'Active',
  createdAt: '2026-01-01T00:00:00Z',
};

function mockList(items: AppUserListItem[]) {
  vi.mocked(UserService.listUsers).mockResolvedValue({ success: true, data: items, message: 'ok' });
}

describe('userStatusForPill / roleLabel', () => {
  it('maps Active, Deactivated, and Terminated for StatusPill', () => {
    expect(userStatusForPill(procurement)).toBe('Active');
    expect(userStatusForPill({ ...procurement, isActive: false, status: 'Inactive' })).toBe('Deactivated');
    expect(userStatusForPill({ ...procurement, isDeleted: true, isActive: false, status: 'Terminated' })).toBe('Terminated');
  });

  it('uses friendly role labels', () => {
    expect(roleLabel('ProcurementManager')).toBe('Procurement Manager');
    expect(roleLabel('RiskAndCompliance')).toBe('Risk & Compliance');
    expect(roleLabel('Viewer')).toBe('Viewer');
    expect(roleLabel('Admin')).toBe('Admin');
  });
});

describe('UserManagement', () => {
  beforeEach(() => {
    asAdminUser();
    vi.mocked(UserService.listUsers).mockReset();
    vi.mocked(UserService.createUser).mockReset();
    vi.mocked(UserService.deactivateUser).mockReset();
    vi.mocked(UserService.activateUser).mockReset();
    vi.mocked(UserService.terminateUser).mockReset();
    mockList([admin, procurement]);
  });

  it('lists users with role and Active status', async () => {
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });

    expect(await screen.findByText('Pat Procure')).toBeInTheDocument();
    expect(screen.getByText('pat@example.com')).toBeInTheDocument();
    expect(screen.getByText('Procurement Manager')).toBeInTheDocument();
    expect(screen.getAllByText('Active').length).toBeGreaterThan(0);
  });

  it('hides Deactivate and Terminate on the logged-in admin row', async () => {
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('System Admin');

    const adminRow = screen.getByText('admin@example.com').closest('tr')!;
    expect(within(adminRow).queryByRole('button', { name: 'Deactivate' })).not.toBeInTheDocument();
    expect(within(adminRow).queryByRole('button', { name: 'Terminate' })).not.toBeInTheDocument();

    const procRow = screen.getByText('pat@example.com').closest('tr')!;
    expect(within(procRow).getByRole('button', { name: 'Deactivate' })).toBeInTheDocument();
    expect(within(procRow).getByRole('button', { name: 'Terminate' })).toBeInTheDocument();
  });

  it('create modal has no Admin role option', async () => {
    const user = userEvent.setup();
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    await user.click(screen.getByRole('button', { name: /Create User/i }));
    const select = await screen.findByLabelText(/Role/i);
    const options = Array.from(select.querySelectorAll('option')).map((o) => o.textContent);
    expect(options).toEqual(['Procurement Manager', 'Risk & Compliance', 'Viewer']);
  });

  it('creates a user and reloads the list', async () => {
    const user = userEvent.setup();
    vi.mocked(UserService.createUser).mockResolvedValue({ success: true, data: 'new-id', message: 'ok' });
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    await user.click(screen.getByRole('button', { name: /Create User/i }));
    await user.type(await screen.findByLabelText(/Display name/i), 'New Proc');
    await user.type(screen.getByLabelText(/Email/i), 'new.proc@example.com');
    await user.type(screen.getByLabelText(/Password/i), 'password1');
    const createButtons = screen.getAllByRole('button', { name: /^Create User$/i });
    await user.click(createButtons[createButtons.length - 1]);

    await waitFor(() => {
      expect(UserService.createUser).toHaveBeenCalledWith(
        expect.objectContaining({
          displayName: 'New Proc',
          email: 'new.proc@example.com',
          role: 'ProcurementManager',
          password: 'password1',
        })
      );
    });
  });

  it('shows a client-side error for a short password', async () => {
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    fireEvent.click(screen.getByRole('button', { name: /Create User/i }));
    fireEvent.change(await screen.findByLabelText(/Display name/i), { target: { value: 'Short User' } });
    fireEvent.change(screen.getByLabelText(/Email/i), { target: { value: 'short@example.com' } });
    fireEvent.change(screen.getByLabelText(/Password/i), { target: { value: 'short' } });
    fireEvent.submit(document.querySelector('.modal.show form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent(/at least 8 characters/i);
    expect(UserService.createUser).not.toHaveBeenCalled();
  });

  it('asks for confirmation before terminate', async () => {
    const user = userEvent.setup();
    vi.mocked(UserService.terminateUser).mockResolvedValue({ success: true, data: true, message: 'ok' });
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    await user.click(screen.getByRole('button', { name: 'Terminate' }));
    expect(await screen.findByText(/This is permanent — Pat Procure will never be able to log in again/i)).toBeInTheDocument();

    await user.click(screen.getByRole('button', { name: 'Confirm' }));
    await waitFor(() => {
      expect(UserService.terminateUser).toHaveBeenCalledWith('proc-id');
    });
  });

  it('does not terminate when the confirmation is cancelled', async () => {
    const user = userEvent.setup();
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    await user.click(screen.getByRole('button', { name: 'Terminate' }));
    await user.click(await screen.findByRole('button', { name: 'Cancel' }));
    expect(UserService.terminateUser).not.toHaveBeenCalled();
  });

  it('calls deactivate for an active user', async () => {
    const user = userEvent.setup();
    vi.mocked(UserService.deactivateUser).mockResolvedValue({ success: true, data: true });
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });
    await screen.findByText('Pat Procure');

    await user.click(screen.getByRole('button', { name: 'Deactivate' }));
    await waitFor(() => expect(UserService.deactivateUser).toHaveBeenCalledWith('proc-id'));
  });

  it('shows Activate and Deactivated for an inactive user', async () => {
    mockList([{ ...procurement, isActive: false, status: 'Inactive' }]);
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });

    expect(await screen.findByText('Deactivated')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Activate' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Terminate' })).toBeInTheDocument();
  });

  it('hides actions for a terminated user', async () => {
    mockList([{ ...procurement, isActive: false, isDeleted: true, status: 'Terminated' }]);
    renderWithRouter(<UserManagement />, { route: '/users', path: '/users' });

    expect(await screen.findByText('Terminated')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Activate' })).not.toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Terminate' })).not.toBeInTheDocument();
  });
});
