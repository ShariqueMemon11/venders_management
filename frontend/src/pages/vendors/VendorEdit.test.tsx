import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { VendorEdit } from './VendorEdit';
import { asProcurementUser, renderWithRouter } from '../../test/test-utils';
import { VendorService } from '../../services/vendorService';

vi.mock('../../services/vendorService', () => ({
  VendorService: {
    getVendorById: vi.fn(),
    updateVendor: vi.fn(),
    terminateVendor: vi.fn(),
  },
}));

const existingVendor = {
  id: 'vendor-1',
  vendorNumber: 'V-100',
  legalName: 'Existing Co',
  tradeName: 'Existing',
  status: 1,
  taxRegistrationNumber: 'TAX-1',
  currencyCode: 'USD',
  website: 'https://example.com',
};

function legalNameInput() {
  const label = screen.getByText(/Legal Entity Name/i);
  const input = label.parentElement?.querySelector('input');
  if (!input) throw new Error('Legal Entity Name input not found');
  return input as HTMLInputElement;
}

describe('VendorEdit', () => {
  beforeEach(() => {
    asProcurementUser();
    vi.mocked(VendorService.getVendorById).mockReset();
    vi.mocked(VendorService.updateVendor).mockReset();
    vi.mocked(VendorService.getVendorById).mockResolvedValue({
      success: true,
      data: existingVendor,
      message: 'ok',
    });
  });

  it('shows a required-field error when Legal Entity Name is cleared on submit', async () => {
    const user = userEvent.setup();
    renderWithRouter(<VendorEdit />, {
      route: '/vendors/vendor-1/edit',
      path: '/vendors/:id/edit',
    });

    const legal = await waitFor(() => legalNameInput());
    await user.clear(legal);

    fireEvent.submit(document.querySelector('form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent(/Legal name is required/i);
    expect(VendorService.updateVendor).not.toHaveBeenCalled();
  });

  it('calls updateVendor and navigates to details on successful submit', async () => {
    const user = userEvent.setup();
    vi.mocked(VendorService.updateVendor).mockResolvedValue({
      success: true,
      data: true,
      message: 'ok',
    });

    renderWithRouter(<VendorEdit />, {
      route: '/vendors/vendor-1/edit',
      path: '/vendors/:id/edit',
    });

    const legal = await waitFor(() => legalNameInput());
    await user.clear(legal);
    await user.type(legal, 'Renamed Co');
    await user.click(screen.getByRole('button', { name: /Save Changes/i }));

    await waitFor(() => {
      expect(VendorService.updateVendor).toHaveBeenCalledTimes(1);
    });
    expect(VendorService.updateVendor).toHaveBeenCalledWith(
      'vendor-1',
      expect.objectContaining({ legalName: 'Renamed Co' })
    );
    expect(await screen.findByTestId('vendor-detail')).toBeInTheDocument();
  });
});
