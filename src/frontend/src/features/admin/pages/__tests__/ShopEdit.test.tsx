import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ShopEdit } from '../ShopEdit';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockShop = {
  id: 'shop-1',
  name: 'Frietjes Gent',
  slug: 'frietjes-gent',
  address: {
    street: 'Veldstraat',
    number: '42',
    city: 'Gent',
    postalCode: '9000',
    country: 'BE',
  },
  contactEmail: 'gent@frietjes.be',
  contactPhone: null,
  isActive: true,
  kitchenDisplayEnabled: false,
  ticketPrinterEnabled: false,
  pushNotificationEnabled: false,
  soundAlertEnabled: false,
  eatIn: { isEnabled: true, requiresTableNumber: true },
  timeSlotOrdering: { isEnabled: false, intervalMinutes: null, maxOrdersPerInterval: null },
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/shops/:shopId"
        element={<ShopEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/shops/shop-1'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:brandSlug/shops/:id', () => HttpResponse.json(mockShop)),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    expect(nameInput).toHaveValue('Frietjes Gent');

    const cityInput = screen.getByLabelText(/^city/i);
    expect(cityInput).toHaveValue('Gent');

    const emailInput = screen.getByLabelText(/contact email/i);
    expect(emailInput).toHaveValue('gent@frietjes.be');
  });

  it('lets the user edit the shop name', async () => {
    const user = userEvent.setup();
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'Frietjes Gent Updated');

    expect(nameInput).toHaveValue('Frietjes Gent Updated');
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:brandSlug/shops/:id', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...mockShop, name: String(capturedBody.name) });
      }),
    );

    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'Frietjes Gent Updated');

    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());
    expect(capturedBody).toMatchObject({
      name: 'Frietjes Gent Updated',
      address: expect.objectContaining({ city: 'Gent' }),
    });
  });

  it('toggles the ticket-printer setting and includes it in the payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:brandSlug/shops/:id', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...mockShop, ticketPrinterEnabled: true });
      }),
    );

    renderPage();

    const printerToggle = await screen.findByLabelText(/auto-print order tickets/i);
    expect(printerToggle).not.toBeChecked();
    await user.click(printerToggle);
    expect(printerToggle).toBeChecked();

    await user.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(capturedBody).not.toBeNull());
    expect(capturedBody).toMatchObject({ ticketPrinterEnabled: true });
  });

  it('renders all four notification channel toggles', async () => {
    renderPage();

    expect(await screen.findByLabelText(/highlight new orders on the kitchen display/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/auto-print order tickets/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/push notifications for new orders/i)).toBeInTheDocument();
    expect(screen.getByLabelText(/sound alert for new orders/i)).toBeInTheDocument();
  });

  it('toggles the new notification channels independently and includes them in the payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:brandSlug/shops/:id', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(mockShop);
      }),
    );

    renderPage();

    // Enable kitchen-display highlight and sound, but leave push off — channels are independent.
    const kitchenToggle = await screen.findByLabelText(/highlight new orders on the kitchen display/i);
    const soundToggle = screen.getByLabelText(/sound alert for new orders/i);
    await user.click(kitchenToggle);
    await user.click(soundToggle);
    expect(kitchenToggle).toBeChecked();
    expect(soundToggle).toBeChecked();
    expect(screen.getByLabelText(/push notifications for new orders/i)).not.toBeChecked();

    await user.click(screen.getByRole('button', { name: /save changes/i }));

    await waitFor(() => expect(capturedBody).not.toBeNull());
    expect(capturedBody).toMatchObject({
      kitchenDisplayEnabled: true,
      soundAlertEnabled: true,
      pushNotificationEnabled: false,
      ticketPrinterEnabled: false,
    });
  });

  it('shows a validation error when name is cleared', async () => {
    const user = userEvent.setup();
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);

    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    expect(await screen.findByText(/name is required/i)).toBeInTheDocument();
  });
});
