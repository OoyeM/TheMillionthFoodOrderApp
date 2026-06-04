/**
 * Tests for ShopChooserPage (US-FP-071):
 * - renders shop cards
 * - auto-redirects when exactly one shop is active
 * - shows empty state when no shops
 */
import { beforeAll, describe, it, expect, vi, type Mock } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import i18next from 'i18next';
import '../../../i18n/config';
import type { StorefrontShop } from '@api/shops';

// ---------------------------------------------------------------------------
// Mock useActiveShops at the module level
// ---------------------------------------------------------------------------

vi.mock('../hooks/useActiveShops', () => ({
  useActiveShops: vi.fn(),
}));

// Import after mock registration
// eslint-disable-next-line import/first
import { useActiveShops } from '../hooks/useActiveShops';
// eslint-disable-next-line import/first
import { ShopChooserPage } from '../pages/ShopChooserPage';

// ---------------------------------------------------------------------------
// i18n bootstrap
// ---------------------------------------------------------------------------

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeShop(overrides: Partial<StorefrontShop> = {}): StorefrontShop {
  return {
    id: 'shop-1',
    name: 'Frietjes Gent',
    slug: 'gent',
    address: {
      street: 'Korenmarkt',
      number: '1',
      city: 'Gent',
      postalCode: '9000',
      country: 'BE',
    },
    isOpen: true,
    eatIn: { isEnabled: true, requiresTableNumber: true },
    ...overrides,
  };
}

function makeQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

interface WrapperOptions {
  shops?: StorefrontShop[];
  loading?: boolean;
  error?: boolean;
}

function renderChooser(options: WrapperOptions = {}) {
  const { shops = [], loading = false, error = false } = options;

  (useActiveShops as Mock).mockReturnValue({
    data: shops,
    isLoading: loading,
    isError: error,
  });

  const queryClient = makeQueryClient();

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={['/frietjes/nl/shops']}>
        <Routes>
          <Route path="/:brandSlug/:lang/shops" element={<ShopChooserPage />} />
          <Route path="/:brandSlug/:lang/:shopSlug/menu" element={<div>MenuPage</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopChooserPage', () => {
  it('shows loading state while fetching', () => {
    renderChooser({ loading: true });
    expect(screen.getByText(i18next.t('loading'))).toBeInTheDocument();
  });

  it('shows error state on fetch failure', () => {
    renderChooser({ error: true });
    expect(screen.getByText(i18next.t('error'))).toBeInTheDocument();
  });

  it('shows empty state when no shops are available', () => {
    renderChooser({ shops: [] });
    expect(screen.getByText(i18next.t('storefront.shopChooser.noShops'))).toBeInTheDocument();
  });

  it('renders a card for each shop when multiple shops exist', () => {
    const shops = [
      makeShop({ id: 'shop-1', name: 'Frietjes Gent', slug: 'gent' }),
      makeShop({ id: 'shop-2', name: 'Frietjes Brugge', slug: 'brugge' }),
    ];
    renderChooser({ shops });
    expect(screen.getByText('Frietjes Gent')).toBeInTheDocument();
    expect(screen.getByText('Frietjes Brugge')).toBeInTheDocument();
  });

  it('renders the shop address in each card', () => {
    // Single shop with a known address — also tests that auto-redirect resolves the address
    // by checking the chooser renders it (two-shop scenario avoids redirect interference)
    const shops = [
      makeShop({ id: 'shop-1', slug: 'gent' }),
      makeShop({
        id: 'shop-2',
        slug: 'brugge',
        name: 'Frietjes Brugge',
        address: {
          street: 'Markt',
          number: '10',
          city: 'Brugge',
          postalCode: '8000',
          country: 'BE',
        },
      }),
    ];
    renderChooser({ shops });
    // Both addresses should appear
    expect(screen.getByText(/Korenmarkt 1/)).toBeInTheDocument();
    expect(screen.getByText(/Markt 10/)).toBeInTheDocument();
  });

  it('auto-redirects to menu when exactly one shop is active', () => {
    const shops = [makeShop({ slug: 'gent' })];
    renderChooser({ shops });
    // Should render the MenuPage stub after the redirect
    expect(screen.getByText('MenuPage')).toBeInTheDocument();
  });
});
