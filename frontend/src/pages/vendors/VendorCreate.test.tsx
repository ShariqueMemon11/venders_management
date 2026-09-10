import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { VendorCreate } from './VendorCreate';
import { asProcurementUser, renderWithRouter } from '../../test/test-utils';
import { VendorService } from '../../services/vendorService';

vi.mock('../../services/vendorService', () => ({
  VendorService: {
    createVendor: vi.fn(),
  },
}));

function legalNameInput() {
  // Labels are siblings (no htmlFor); locate by heading context + first form text control.
  const label = screen.getByText(/Legal Entity Name/i);
  const input = label.parentElement?.querySelector('input');
  if (!input) throw new Error('Legal Entity Name input not found');
  return input as HTMLInputElement;
}

describe('VendorCreate', () => {
  beforeEach(() => {
    asProcurementUser();
    vi.mocked(VendorService.createVendor).mockReset();
  });

  it('shows a required-field error when Legal Entity Name is empty on submit', async () => {
    renderWithRouter(<VendorCreate />, { route: '/vendors/new', path: '/vendors/new' });

    // Bypass HTML5 constraint validation so the React handler runs.
    fireEvent.submit(document.querySelector('form')!);

    expect(await screen.findByRole('alert')).toHaveTextContent(/Legal name is required/i);
    expect(VendorService.createVendor).not.toHaveBeenCalled();
  });

  it('calls createVendor and navigates to the new vendor on successful submit', async () => {
    const user = userEvent.setup();
    vi.mocked(VendorService.createVendor).mockResolvedValue({
      success: true,
      data: 'new-vendor-id-123',
      message: 'ok',
    });

    renderWithRouter(<VendorCreate />, { route: '/vendors/new', path: '/vendors/new' });

    await user.type(legalNameInput(), 'Acme Supplies Ltd');
    await user.click(screen.getByRole('button', { name: /Save Draft/i }));

    await waitFor(() => {
      expect(VendorService.createVendor).toHaveBeenCalledTimes(1);
    });
    expect(VendorService.createVendor).toHaveBeenCalledWith(
      expect.objectContaining({
        legalName: 'Acme Supplies Ltd',
        currencyCode: 'USD',
      })
    );
    expect(await screen.findByTestId('vendor-detail')).toBeInTheDocument();
  });
});
