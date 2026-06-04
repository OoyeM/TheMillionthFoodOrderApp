/**
 * t-17 — AC4 real-Dashboard eat-in guard
 *
 * Renders the ACTUAL PosDashboard component (not a wrapper replica) and verifies:
 *
 * 1. With EatIn selected and NO table number, the "Place Order" button is disabled
 *    and the validation alert is shown.
 * 2. After entering a valid table number, the button becomes enabled and the
 *    alert disappears.
 * 3. Switching back to Pickup (with a filled cart) re-enables the button immediately.
 *
 * This test uses the real Dashboard.tsx guard logic (lines 29-32) rather than
 * a SubmitGuardWrapper replica, so any divergence between the test and the real
 * component would be caught.
 */
// Initialize i18n before any component so t() resolves to NL translations
import '@/i18n/config';

import { describe, it, expect, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/msw/server';
import { AuthContext, type AuthContextValue } from '@/auth/AuthContext';
import type { UserRole } from '@/types/auth';
import { PosDashboard } from '../pages/Dashboard';

// ── Auth mock ─────────────────────────────────────────────────────────────────

function makeStaffAuth(): AuthContextValue {
  return {
    isAuthenticated: true,
    isLoading: false,
    user: {
      userId: 'staff-1',
      displayName: 'Counter Staff',
      email: 'staff@frietjes.be',
      roles: ['counter-staff' as UserRole],
      brandSlug: 'frietjes',
      firstName: null,
      lastName: null,
      phoneNumber: null,
    },
    login: () => { /* no-op */ },
    logout: () => Promise.resolve(),
    hasRole: (role) => role === 'counter-staff',
    hasAnyRole: (roles) => roles.includes('counter-staff'),
  };
}

// ── Render helper ─────────────────────────────────────────────────────────────

/**
 * Renders the real PosDashboard inside a route tree that matches the production
 * URL shape (/:brandSlug/:lang/pos) so that useParams resolves correctly.
 */
function renderDashboard() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={['/frietjes/nl/pos']}>
        <AuthContext.Provider value={makeStaffAuth()}>
          <Routes>
            {/*
             * The route must match /:brandSlug/:lang/pos so that useParams()
             * inside PosDashboard and PosDashboardInner returns { brandSlug, lang }.
             * shopId is optional and falls back to 'shop-1' inside PosDashboard.
             */}
            <Route path="/:brandSlug/:lang/pos" element={<PosDashboard />} />
            <Route
              path="/:brandSlug/:lang/pos/confirmation/:orderNumber"
              element={<div data-testid="confirmation-page">Confirmed</div>}
            />
          </Routes>
        </AuthContext.Provider>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('PosDashboard — EatIn submit guard (t-17, AC4)', () => {
  afterEach(() => server.resetHandlers());

  it('Place Order button is disabled when no items are in the cart', async () => {
    renderDashboard();

    await waitFor(() => {
      expect(screen.getByTestId('pos-menu-grid')).toBeInTheDocument();
    });

    // The submit button is rendered in PosDashboardInner; canSubmit = items.length > 0 && ...
    // With an empty cart the button must be disabled regardless of order type.
    const submitButton = screen.getByRole('button', { name: /bestelling plaatsen/i });
    expect(submitButton).toBeDisabled();
  });

  it('Place Order button is disabled when EatIn is selected but table number is missing', async () => {
    // Override category products to provide a tappable simple product
    server.use(
      http.get('/api/brands/:slug/menu-categories/:id/products', () =>
        HttpResponse.json([
          {
            id: 'prod-simple',
            productType: 'Simple',
            name: 'Kleine friet',
            basePrice: { amount: 3.5, currency: 'EUR' },
            imageUrl: null,
            menuCategoryId: 'cat-1',
            sortOrderInCategory: 1,
            allergens: [],
            dietaryTags: [],
            createdAt: '2024-01-01T00:00:00Z',
          },
        ]),
      ),
      // Ensure no modifiers so the product is added directly (no modal)
      http.get('/api/brands/:slug/products/:productId/modifier-groups', () =>
        HttpResponse.json([]),
      ),
    );

    renderDashboard();

    // Wait for product tile to be clickable
    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    // Add item via the real product grid — no modal because no modifiers
    fireEvent.click(screen.getByText('Kleine friet'));

    // Switch to EatIn (the PosOrderPanel renders inside PosDashboardInner)
    await waitFor(() => {
      expect(screen.getByTestId('order-type-eatin')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('order-type-eatin'));

    // With EatIn selected and no table number the submit button must be disabled
    await waitFor(() => {
      const submitButton = screen.getByRole('button', { name: /bestelling plaatsen/i });
      expect(submitButton).toBeDisabled();
    });

    // The validation alert must be shown inside the real Dashboard DOM
    await waitFor(() => {
      // Dashboard renders role="alert" when isEatInMissingTable && items.length > 0
      const alerts = screen.getAllByRole('alert');
      // At least one alert contains the table-number error text
      const tableAlert = alerts.find((el) =>
        el.textContent?.toLowerCase().includes('tafelnummer'),
      );
      expect(tableAlert).toBeDefined();
    });
  });

  it('Place Order button is enabled after entering a table number for EatIn', async () => {
    server.use(
      http.get('/api/brands/:slug/menu-categories/:id/products', () =>
        HttpResponse.json([
          {
            id: 'prod-simple',
            productType: 'Simple',
            name: 'Kleine friet',
            basePrice: { amount: 3.5, currency: 'EUR' },
            imageUrl: null,
            menuCategoryId: 'cat-1',
            sortOrderInCategory: 1,
            allergens: [],
            dietaryTags: [],
            createdAt: '2024-01-01T00:00:00Z',
          },
        ]),
      ),
      http.get('/api/brands/:slug/products/:productId/modifier-groups', () =>
        HttpResponse.json([]),
      ),
    );

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    // Add item
    fireEvent.click(screen.getByText('Kleine friet'));

    // Switch to EatIn
    await waitFor(() => {
      expect(screen.getByTestId('order-type-eatin')).toBeInTheDocument();
    });
    fireEvent.click(screen.getByTestId('order-type-eatin'));

    // Table number input must appear
    await waitFor(() => {
      expect(screen.getByTestId('table-number-input')).toBeInTheDocument();
    });

    // Button is still disabled (no table yet)
    expect(screen.getByRole('button', { name: /bestelling plaatsen/i })).toBeDisabled();

    // Enter a valid table number
    fireEvent.change(screen.getByTestId('table-number-input'), { target: { value: '3' } });

    // Now the submit button must be enabled
    await waitFor(() => {
      expect(screen.getByRole('button', { name: /bestelling plaatsen/i })).not.toBeDisabled();
    });

    // The table-number validation alert must have disappeared
    await waitFor(() => {
      const alerts = screen.queryAllByRole('alert');
      const tableAlert = alerts.find((el) =>
        el.textContent?.toLowerCase().includes('tafelnummer'),
      );
      expect(tableAlert).toBeUndefined();
    });
  });

  it('switching from EatIn back to Pickup re-enables the button without needing a table number', async () => {
    server.use(
      http.get('/api/brands/:slug/menu-categories/:id/products', () =>
        HttpResponse.json([
          {
            id: 'prod-simple',
            productType: 'Simple',
            name: 'Kleine friet',
            basePrice: { amount: 3.5, currency: 'EUR' },
            imageUrl: null,
            menuCategoryId: 'cat-1',
            sortOrderInCategory: 1,
            allergens: [],
            dietaryTags: [],
            createdAt: '2024-01-01T00:00:00Z',
          },
        ]),
      ),
      http.get('/api/brands/:slug/products/:productId/modifier-groups', () =>
        HttpResponse.json([]),
      ),
    );

    renderDashboard();

    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Kleine friet'));

    await waitFor(() => {
      expect(screen.getByTestId('order-type-eatin')).toBeInTheDocument();
    });

    // Switch to EatIn without entering table — button disabled
    fireEvent.click(screen.getByTestId('order-type-eatin'));

    await waitFor(() => {
      expect(screen.getByRole('button', { name: /bestelling plaatsen/i })).toBeDisabled();
    });

    // Switch back to Pickup
    fireEvent.click(screen.getByTestId('order-type-pickup'));

    // Table number input should disappear and button should be enabled
    await waitFor(() => {
      expect(screen.queryByTestId('table-number-input')).not.toBeInTheDocument();
      expect(screen.getByRole('button', { name: /bestelling plaatsen/i })).not.toBeDisabled();
    });
  });
});
