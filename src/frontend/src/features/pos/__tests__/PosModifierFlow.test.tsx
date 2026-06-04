/**
 * t-16 — AC5 end-to-end UI modifier flow
 *
 * Verifies that modifiers selected via PosModifierModal are correctly
 * transformed into selectedModifierIds in the final POST payload.
 *
 * This test exercises the FULL UI path:
 *   PosMenuGrid (tap product) → PosModifierModal (toggle modifier checkbox) →
 *   confirm → item lands in PosOrderContext → useCreateInStoreOrder builds POST body →
 *   MSW captures the request → assert selectedModifierIds reflects UI selection
 *
 * This is NOT a fixture test — the modifier IDs come from actual UI interaction
 * with PosModifierModal, not from manually constructed CartItem objects.
 */
// Initialize i18n before any component so t() resolves to NL translations
import '@/i18n/config';

import { describe, it, expect, afterEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/msw/server';
import { renderWithProviders } from '@/test/testUtils';
import { AuthContext, type AuthContextValue } from '@/auth/AuthContext';
import type { UserRole } from '@/types/auth';
import { PosOrderProvider, useOrderState } from '../context/PosOrderContext';
import { useCreateInStoreOrder } from '../hooks/useCreateInStoreOrder';
import { PosMenuGrid } from '../components/PosMenuGrid';

// ── Counter-staff auth context ────────────────────────────────────────────────

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

// ── MSW handler setup helpers ─────────────────────────────────────────────────

const productWithModifiers = {
  id: 'prod-friet',
  productType: 'Simple',
  name: 'Grote friet',
  basePrice: { amount: 4.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 1,
  allergens: [],
  dietaryTags: [],
  createdAt: '2024-01-01T00:00:00Z',
};

/**
 * Override category products to return a single product that has modifier groups.
 */
function setupProductWithModifiers() {
  server.use(
    http.get('/api/brands/:slug/menu-categories/:id/products', () =>
      HttpResponse.json([productWithModifiers]),
    ),
    // Two modifiers: Mayonaise (mod-mayo) and Ketchup (mod-ketchup)
    http.get('/api/brands/:slug/products/:productId/modifier-groups', ({ params }) => {
      if (params.productId === 'prod-friet') {
        return HttpResponse.json([
          {
            modifierGroupId: 'mg-sauzen',
            name: 'Sauzen',
            sortOrder: 1,
            modifiers: [
              {
                id: 'mod-mayo',
                priceAdjustment: 0,
                sortOrder: 1,
                translations: [{ languageCode: 'nl', name: 'Mayonaise' }],
              },
              {
                id: 'mod-ketchup',
                priceAdjustment: 0.5,
                sortOrder: 2,
                translations: [{ languageCode: 'nl', name: 'Ketchup' }],
              },
            ],
          },
        ]);
      }
      return HttpResponse.json([]);
    }),
  );
}

// ── Submit button component (wires useCreateInStoreOrder to the order state) ──

/**
 * Minimal submit trigger rendered next to PosMenuGrid so the test can
 * call mutateAsync without mounting the full PosDashboard (which requires
 * a full route tree and navigation context).
 */
function SubmitTrigger({
  brandSlug,
  shopId,
  onCaptured,
}: {
  brandSlug: string;
  shopId: string;
  onCaptured: (body: Record<string, unknown>) => void;
}) {
  const { state } = useOrderState();
  const mutation = useCreateInStoreOrder(brandSlug, shopId);

  return (
    <div>
      <span data-testid="item-count">{state.items.length}</span>
      <button
        type="button"
        data-testid="submit-order"
        disabled={state.items.length === 0 || mutation.isPending}
        onClick={() => {
          void mutation.mutateAsync({}).then((order) => {
            // Surface the order for assertions
            onCaptured({ orderNumber: order.orderNumber } as Record<string, unknown>);
          });
        }}
      >
        Bestelling plaatsen
      </button>
      {mutation.isSuccess && <span data-testid="order-success">OK</span>}
      {mutation.isError && <span data-testid="order-error">ERROR</span>}
    </div>
  );
}

// ── Test ─────────────────────────────────────────────────────────────────────

describe('POS modifier flow — end-to-end UI (t-16, AC5)', () => {
  afterEach(() => server.resetHandlers());

  it('selects a modifier via PosModifierModal UI and the POST body reflects selectedModifierIds', async () => {
    setupProductWithModifiers();

    let capturedBody: {
      items?: Array<{ productId: string; quantity: number; selectedModifierIds: string[] }>;
      orderType?: string;
      paymentMethod?: string;
    } | null = null;

    // Override the in-store endpoint to capture the request body
    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedBody = await request.json() as typeof capturedBody;
        return HttpResponse.json(
          {
            id: 'order-modifier-test',
            orderNumber: 'ORD-100',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: 'Pickup',
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 6,
            subtotalGross: 4.5,
            totalVatAmount: 0.25,
            totalNet: 4.25,
            totalGross: 4.5,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
          },
          { status: 201 },
        );
      }),
    );

    const auth = makeStaffAuth();

    renderWithProviders(
      <AuthContext.Provider value={auth}>
        <PosOrderProvider>
          <PosMenuGrid brandSlug="frietjes" />
          <SubmitTrigger
            brandSlug="frietjes"
            shopId="shop-1"
            onCaptured={() => { /* captured via server.use */ }}
          />
        </PosOrderProvider>
      </AuthContext.Provider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    // Step 1: Wait for the product tile to appear (confirms categories + products loaded)
    await waitFor(() => {
      expect(screen.getByText('Grote friet')).toBeInTheDocument();
    });

    // Step 2: Wait for the modifier indicator to confirm the modifier query resolved
    await waitFor(() => {
      expect(screen.getByText('+ opties')).toBeInTheDocument();
    });

    // Step 3: Tap the product tile — PosModifierModal should open
    fireEvent.click(screen.getByText('Grote friet'));

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    // Step 4: Wait for modifiers to load inside the modal
    await waitFor(() => {
      expect(screen.getByText('Sauzen')).toBeInTheDocument();
      expect(screen.getByLabelText(/Mayonaise/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Ketchup/i)).toBeInTheDocument();
    });

    // Step 5: Toggle Mayonaise modifier (check the checkbox)
    const mayoCheckbox = screen.getByLabelText(/Mayonaise/i);
    expect(mayoCheckbox).not.toBeChecked();
    fireEvent.click(mayoCheckbox);
    expect(mayoCheckbox).toBeChecked();

    // Ketchup remains unchecked — only Mayonaise should appear in the payload
    expect(screen.getByLabelText(/Ketchup/i)).not.toBeChecked();

    // Step 6: Confirm the modifier selection
    fireEvent.click(screen.getByTestId('pos-modifier-confirm'));

    // Step 7: Modal should close and item should be in the order
    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    await waitFor(() => {
      expect(screen.getByTestId('item-count').textContent).toBe('1');
    });

    // Step 8: Submit the order
    fireEvent.click(screen.getByTestId('submit-order'));

    await waitFor(() => {
      expect(screen.getByTestId('order-success')).toBeInTheDocument();
    });

    // Step 9: Assert the POST body contains the correct selectedModifierIds
    expect(capturedBody).not.toBeNull();
    expect(capturedBody!.items).toHaveLength(1);

    const item = capturedBody!.items![0]!;
    expect(item.productId).toBe('prod-friet');
    // The modifier selected via the UI checkbox must appear here — NOT from a fixture
    expect(item.selectedModifierIds).toEqual(['mod-mayo']);
    // Ketchup was NOT checked, so it must not appear
    expect(item.selectedModifierIds).not.toContain('mod-ketchup');
  });

  it('selecting NO modifiers results in an empty selectedModifierIds array', async () => {
    setupProductWithModifiers();

    let capturedBody: {
      items?: Array<{ productId: string; quantity: number; selectedModifierIds: string[] }>;
    } | null = null;

    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedBody = await request.json() as typeof capturedBody;
        return HttpResponse.json(
          {
            id: 'order-no-mod',
            orderNumber: 'ORD-101',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: 'Pickup',
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 6,
            subtotalGross: 4.5,
            totalVatAmount: 0.25,
            totalNet: 4.25,
            totalGross: 4.5,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
          },
          { status: 201 },
        );
      }),
    );

    const auth = makeStaffAuth();

    renderWithProviders(
      <AuthContext.Provider value={auth}>
        <PosOrderProvider>
          <PosMenuGrid brandSlug="frietjes" />
          <SubmitTrigger
            brandSlug="frietjes"
            shopId="shop-1"
            onCaptured={() => { /* no-op */ }}
          />
        </PosOrderProvider>
      </AuthContext.Provider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Grote friet')).toBeInTheDocument();
      expect(screen.getByText('+ opties')).toBeInTheDocument();
    });

    // Tap product — modal opens
    fireEvent.click(screen.getByText('Grote friet'));

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByLabelText(/Mayonaise/i)).toBeInTheDocument();
    });

    // Confirm without checking any modifier
    fireEvent.click(screen.getByTestId('pos-modifier-confirm'));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
      expect(screen.getByTestId('item-count').textContent).toBe('1');
    });

    // Submit
    fireEvent.click(screen.getByTestId('submit-order'));

    await waitFor(() => {
      expect(screen.getByTestId('order-success')).toBeInTheDocument();
    });

    expect(capturedBody).not.toBeNull();
    expect(capturedBody!.items![0]!.selectedModifierIds).toEqual([]);
  });

  it('selecting multiple modifiers includes all their IDs in the payload', async () => {
    setupProductWithModifiers();

    let capturedBody: {
      items?: Array<{ productId: string; quantity: number; selectedModifierIds: string[] }>;
    } | null = null;

    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedBody = await request.json() as typeof capturedBody;
        return HttpResponse.json(
          {
            id: 'order-multi-mod',
            orderNumber: 'ORD-102',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: 'Pickup',
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 6,
            subtotalGross: 4.5,
            totalVatAmount: 0.25,
            totalNet: 4.25,
            totalGross: 4.5,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
          },
          { status: 201 },
        );
      }),
    );

    const auth = makeStaffAuth();

    renderWithProviders(
      <AuthContext.Provider value={auth}>
        <PosOrderProvider>
          <PosMenuGrid brandSlug="frietjes" />
          <SubmitTrigger
            brandSlug="frietjes"
            shopId="shop-1"
            onCaptured={() => { /* no-op */ }}
          />
        </PosOrderProvider>
      </AuthContext.Provider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Grote friet')).toBeInTheDocument();
      expect(screen.getByText('+ opties')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Grote friet'));

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByLabelText(/Mayonaise/i)).toBeInTheDocument();
      expect(screen.getByLabelText(/Ketchup/i)).toBeInTheDocument();
    });

    // Check both modifiers
    fireEvent.click(screen.getByLabelText(/Mayonaise/i));
    fireEvent.click(screen.getByLabelText(/Ketchup/i));

    expect(screen.getByLabelText(/Mayonaise/i)).toBeChecked();
    expect(screen.getByLabelText(/Ketchup/i)).toBeChecked();

    fireEvent.click(screen.getByTestId('pos-modifier-confirm'));

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
    });

    fireEvent.click(screen.getByTestId('submit-order'));

    await waitFor(() => {
      expect(screen.getByTestId('order-success')).toBeInTheDocument();
    });

    expect(capturedBody).not.toBeNull();
    const ids = capturedBody!.items![0]!.selectedModifierIds;
    expect(ids).toHaveLength(2);
    expect(ids).toContain('mod-mayo');
    expect(ids).toContain('mod-ketchup');
  });
});
