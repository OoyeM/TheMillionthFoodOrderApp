import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ShopList } from '../ShopList';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockShop = {
  id: 'shop-1',
  name: 'Gent Centrum',
  slug: 'gent-centrum',
  address: {
    street: 'Veldstraat',
    number: '1',
    city: 'Gent',
    postalCode: '9000',
    country: 'BE',
  },
  contactEmail: 'gent@frietjes.be',
  contactPhone: null,
  isActive: true,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/shops" element={<ShopList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/shops'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/shops', () =>
      HttpResponse.json([mockShop]),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopList', () => {
  it('renders shop name "Gent Centrum" after load', async () => {
    renderPage();

    const shopName = await screen.findByText('Gent Centrum');
    expect(shopName).toBeInTheDocument();
  });

  it('Create Shop button is present', async () => {
    renderPage();

    // Wait for the page to load before checking the button
    await screen.findByText('Gent Centrum');

    const createButton = screen.getByRole('button', { name: /\+ create shop/i });
    expect(createButton).toBeInTheDocument();
  });

  it('clicking a row navigates to the edit route', async () => {
    const user = userEvent.setup();

    // Add a second route to confirm navigation lands on the shop edit URL
    const { container } = renderWithProviders(
      <Routes>
        <Route path="/:brandSlug/:lang/admin/shops" element={<ShopList />} />
        <Route
          path="/:brandSlug/:lang/admin/shops/:shopId"
          element={<div data-testid="shop-edit-page">Shop Edit</div>}
        />
      </Routes>,
      { initialEntries: ['/frietjes/nl/admin/shops'] },
    );

    // Wait for the shop row to appear
    const shopNameCell = await screen.findByText('Gent Centrum');

    // Click the row (the td containing the name)
    await user.click(shopNameCell);

    // The router should have navigated to the edit page
    await waitFor(() => {
      expect(container.querySelector('[data-testid="shop-edit-page"]')).toBeInTheDocument();
    });
  });

  it('Deactivate button calls the deactivate API', async () => {
    const user = userEvent.setup();

    let deactivateCalled = false;
    server.use(
      http.post('/api/brands/:slug/shops/:shopId/deactivate', () => {
        deactivateCalled = true;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderPage();

    // Wait for the shop row to render
    await screen.findByText('Gent Centrum');

    const deactivateButton = screen.getByRole('button', { name: /deactivate/i });
    await user.click(deactivateButton);

    await waitFor(() => {
      expect(deactivateCalled).toBe(true);
    });
  });
});
