import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { BrandList } from '../BrandList';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const activeBrand = {
  id: 'brand-1',
  slug: 'frietjes',
  name: 'Frietjes?',
  contactEmail: 'admin@frietjes.be',
  contactPhone: null,
  isActive: true,
  databaseName: 'BrandDb_frietjes',
  staffAuthMethod: 'EmailPassword',
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

const inactiveBrand = {
  ...activeBrand,
  isActive: false,
};

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/brands" element={<BrandList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/brands'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands', () => HttpResponse.json([activeBrand])),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('BrandList', () => {
  it('renders the brand list with fetched data', async () => {
    renderPage();

    // Wait for the async data to load and the brand name to appear in the DOM
    const brandName = await screen.findByText('Frietjes?');
    expect(brandName).toBeInTheDocument();
  });

  it('deactivate button calls the deactivate API', async () => {
    const user = userEvent.setup();

    let deactivateCalled = false;
    server.use(
      http.get('/api/brands', () => HttpResponse.json([activeBrand])),
      http.post('/api/brands/:id/deactivate', ({ params }) => {
        if (params.id === 'brand-1') {
          deactivateCalled = true;
        }
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderPage();

    // Wait for the brand row to appear
    await screen.findByText('Frietjes?');

    // Click the Deactivate button for the active brand
    const deactivateButton = screen.getByRole('button', { name: /deactivate/i });
    await user.click(deactivateButton);

    await waitFor(() => expect(deactivateCalled).toBe(true));
  });

  it('activate button calls the activate API', async () => {
    const user = userEvent.setup();

    let activateCalled = false;
    server.use(
      http.get('/api/brands', () => HttpResponse.json([inactiveBrand])),
      http.post('/api/brands/:id/activate', ({ params }) => {
        if (params.id === 'brand-1') {
          activateCalled = true;
        }
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderPage();

    // Wait for the brand row to appear
    await screen.findByText('Frietjes?');

    // Click the Activate button for the inactive brand
    const activateButton = screen.getByRole('button', { name: /^activate$/i });
    await user.click(activateButton);

    await waitFor(() => expect(activateCalled).toBe(true));
  });

  it('Create Brand button is present', async () => {
    renderPage();

    // The button is rendered immediately (not async), but wait for mount
    const createButton = await screen.findByRole('button', { name: /\+ create brand/i });
    expect(createButton).toBeInTheDocument();
  });
});
