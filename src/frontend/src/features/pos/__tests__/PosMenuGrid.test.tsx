/**
 * t-9 — PosMenuGrid renders a touch grid of products [AC1]
 * t-10 — Product interactions: modifiers open modal, simple adds directly,
 *         combo adds as single line, re-adding increments qty [AC2]
 */
// Import i18n config before components so that t() resolves to NL translations (fallback)
import '@/i18n/config';

import { describe, it, expect, afterEach } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/msw/server';
import { renderWithProviders } from '@/test/testUtils';
import { PosOrderProvider, useOrderState } from '../context/PosOrderContext';
import { PosMenuGrid } from '../components/PosMenuGrid';

// Helper: renders PosMenuGrid inside a PosOrderProvider + QueryClient + Router
function renderMenuGrid(brandSlug = 'frietjes') {
  return renderWithProviders(
    <PosOrderProvider>
      <PosMenuGrid brandSlug={brandSlug} />
    </PosOrderProvider>,
    { initialEntries: ['/frietjes/nl/pos'] },
  );
}

import type { ProductListItem } from '@/types/common';

// Minimal fixtures
const simpleProduct: ProductListItem = {
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
};

const productWithModifiers: ProductListItem = {
  id: 'prod-modifiers',
  productType: 'Simple',
  name: 'Grote friet',
  basePrice: { amount: 4.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 2,
  allergens: [],
  dietaryTags: [],
  createdAt: '2024-01-01T00:00:00Z',
};

const comboProduct: ProductListItem = {
  id: 'prod-combo',
  productType: 'Combo',
  name: 'Friet Menu',
  basePrice: { amount: 7.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 3,
  allergens: [],
  dietaryTags: [],
  createdAt: '2024-01-01T00:00:00Z',
};

/**
 * Override menu-categories products to return test fixtures.
 */
function setupProductsHandler(products: import('@/types/common').ProductListItem[]) {
  server.use(
    http.get('/api/brands/:slug/menu-categories/:id/products', () =>
      HttpResponse.json(products),
    ),
  );
}

/**
 * Override modifier groups to return groups WITH modifiers for a product.
 */
function setupWithModifiersHandler(productId: string) {
  server.use(
    http.get('/api/brands/:slug/products/:productId/modifier-groups', ({ params }) => {
      if (params.productId === productId) {
        return HttpResponse.json([
          {
            modifierGroupId: 'mg-1',
            name: 'Sauzen',
            sortOrder: 1,
            modifiers: [
              {
                id: 'mod-1',
                priceAdjustment: 0,
                sortOrder: 1,
                translations: [{ languageCode: 'nl', name: 'Mayonaise' }],
              },
            ],
          },
        ]);
      }
      return HttpResponse.json([]);
    }),
  );
}

/**
 * Override modifier groups to return empty for a product (no modifiers).
 */
function setupNoModifiersHandler() {
  server.use(
    http.get('/api/brands/:slug/products/:productId/modifier-groups', () =>
      HttpResponse.json([]),
    ),
  );
}

describe('PosMenuGrid (t-9)', () => {
  afterEach(() => server.resetHandlers());

  it('renders a grid of product tiles from the menu', async () => {
    setupProductsHandler([simpleProduct, productWithModifiers]);
    setupNoModifiersHandler();

    renderMenuGrid();

    // Wait for the category name "Frietjes" (from the default MSW handler)
    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    expect(screen.getByText('Grote friet')).toBeInTheDocument();

    // Grid should be visible
    expect(screen.getByTestId('pos-menu-grid')).toBeInTheDocument();
  });

  it('renders formatted prices in nl-BE currency format', async () => {
    setupProductsHandler([simpleProduct]);
    setupNoModifiersHandler();

    renderMenuGrid();

    await waitFor(() => {
      // nl-BE format: € 3,50
      expect(screen.getByText(/3,50/)).toBeInTheDocument();
    });
  });
});

describe('PosMenuGrid product interactions (t-10)', () => {
  afterEach(() => server.resetHandlers());

  it('tapping a product without modifiers adds it directly to the order', async () => {
    setupProductsHandler([simpleProduct]);
    setupNoModifiersHandler();

    // Render with a state spy component
    let addedItems: string[] = [];
    function OrderSpy() {
      const { state } = useOrderState();
      addedItems = state.items.map((i) => i.productId);
      return null;
    }

    renderWithProviders(
      <PosOrderProvider>
        <PosMenuGrid brandSlug="frietjes" />
        <OrderSpy />
      </PosOrderProvider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    // Click the product tile
    fireEvent.click(screen.getByText('Kleine friet'));

    await waitFor(() => {
      expect(addedItems).toContain('prod-simple');
    });
  });

  it('tapping a product with modifiers opens PosModifierModal with the correct product', async () => {
    setupProductsHandler([productWithModifiers]);
    setupWithModifiersHandler('prod-modifiers');

    renderMenuGrid();

    // Wait for product tile AND the modifier indicator (confirms modifier query resolved)
    await waitFor(() => {
      expect(screen.getByText('Grote friet')).toBeInTheDocument();
      expect(screen.getByText('+ opties')).toBeInTheDocument();
    });

    // Click product tile — modal should appear
    fireEvent.click(screen.getByText('Grote friet'));

    await waitFor(() => {
      // Modal dialog role should appear
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    // Modal should show the correct product name and its modifier group
    await waitFor(() => {
      // Product name appears inside the modal dialog
      expect(screen.getByRole('dialog')).toBeInTheDocument();
      expect(screen.getByText('Sauzen')).toBeInTheDocument();
    });
  });

  it('confirming modifier modal adds item with selected modifiers', async () => {
    setupProductsHandler([productWithModifiers]);
    setupWithModifiersHandler('prod-modifiers');

    let orderItems: Array<{ productId: string; selectedModifiers: unknown[] }> = [];
    function OrderSpy() {
      const { state } = useOrderState();
      orderItems = state.items.map((i) => ({
        productId: i.productId,
        selectedModifiers: i.selectedModifiers,
      }));
      return null;
    }

    renderWithProviders(
      <PosOrderProvider>
        <PosMenuGrid brandSlug="frietjes" />
        <OrderSpy />
      </PosOrderProvider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    // Wait for product tile AND the modifier indicator (confirms modifier query resolved)
    await waitFor(() => {
      expect(screen.getByText('Grote friet')).toBeInTheDocument();
      expect(screen.getByText('+ opties')).toBeInTheDocument();
    });

    // Open modal
    fireEvent.click(screen.getByText('Grote friet'));

    await waitFor(() => {
      expect(screen.getByRole('dialog')).toBeInTheDocument();
    });

    // Wait for modifiers to load (inside the modal, rendered by PosModifierModal)
    await waitFor(() => {
      expect(screen.getByText('Sauzen')).toBeInTheDocument();
    });

    // Click the confirm/add button
    fireEvent.click(screen.getByTestId('pos-modifier-confirm'));

    await waitFor(() => {
      expect(orderItems.some((i) => i.productId === 'prod-modifiers')).toBe(true);
    });

    // Modal should be closed
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('adding a combo product adds it as a single line (no component breakdown)', async () => {
    setupProductsHandler([comboProduct]);
    setupNoModifiersHandler();

    let orderItems: Array<{ productId: string; quantity: number }> = [];
    function OrderSpy() {
      const { state } = useOrderState();
      orderItems = state.items.map((i) => ({ productId: i.productId, quantity: i.quantity }));
      return null;
    }

    renderWithProviders(
      <PosOrderProvider>
        <PosMenuGrid brandSlug="frietjes" />
        <OrderSpy />
      </PosOrderProvider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Friet Menu')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Friet Menu'));

    await waitFor(() => {
      expect(orderItems).toHaveLength(1);
      expect(orderItems[0]?.productId).toBe('prod-combo');
      expect(orderItems[0]?.quantity).toBe(1);
    });
  });

  it('re-adding the same product increments its quantity', async () => {
    setupProductsHandler([simpleProduct]);
    setupNoModifiersHandler();

    let orderItems: Array<{ productId: string; quantity: number }> = [];
    function OrderSpy() {
      const { state } = useOrderState();
      orderItems = state.items.map((i) => ({ productId: i.productId, quantity: i.quantity }));
      return null;
    }

    renderWithProviders(
      <PosOrderProvider>
        <PosMenuGrid brandSlug="frietjes" />
        <OrderSpy />
      </PosOrderProvider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Kleine friet')).toBeInTheDocument();
    });

    fireEvent.click(screen.getByText('Kleine friet'));
    fireEvent.click(screen.getByText('Kleine friet'));

    await waitFor(() => {
      expect(orderItems).toHaveLength(1);
      expect(orderItems[0]?.quantity).toBe(2);
    });
  });

  it('re-adding the same combo product increments quantity (single line, not two lines)', async () => {
    setupProductsHandler([comboProduct]);
    setupNoModifiersHandler();

    let orderItems: Array<{ productId: string; quantity: number }> = [];
    function OrderSpy() {
      const { state } = useOrderState();
      orderItems = state.items.map((i) => ({ productId: i.productId, quantity: i.quantity }));
      return null;
    }

    renderWithProviders(
      <PosOrderProvider>
        <PosMenuGrid brandSlug="frietjes" />
        <OrderSpy />
      </PosOrderProvider>,
      { initialEntries: ['/frietjes/nl/pos'] },
    );

    await waitFor(() => {
      expect(screen.getByText('Friet Menu')).toBeInTheDocument();
    });

    // Add combo twice
    fireEvent.click(screen.getByText('Friet Menu'));
    fireEvent.click(screen.getByText('Friet Menu'));

    await waitFor(() => {
      // Must remain a single line with qty=2, not two separate lines
      expect(orderItems).toHaveLength(1);
      expect(orderItems[0]?.productId).toBe('prod-combo');
      expect(orderItems[0]?.quantity).toBe(2);
    });
  });
});
