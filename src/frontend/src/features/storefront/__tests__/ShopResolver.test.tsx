/**
 * Tests for ShopResolver (US-FP-071):
 * - resolves :shopSlug to a shop and provides it via context
 * - redirects to shop chooser when slug is not found
 * - shows loading / error states
 */
import { beforeAll, describe, it, expect, vi, type Mock } from 'vitest';
import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import i18next from 'i18next';
import '../../../i18n/config';
import type { StorefrontShop } from '@api/shops';

// ---------------------------------------------------------------------------
// Mock useActiveShops at the module level so imports see the mocked version
// ---------------------------------------------------------------------------

vi.mock('../hooks/useActiveShops', () => ({
  useActiveShops: vi.fn(),
}));

// We need to import AFTER the mock is registered
// eslint-disable-next-line import/first
import { useActiveShops } from '../hooks/useActiveShops';
// eslint-disable-next-line import/first
import { ShopResolver } from '../context/ShopContext';
// eslint-disable-next-line import/first
import { useResolvedShop } from '../hooks/useResolvedShop';

// ---------------------------------------------------------------------------
// i18n bootstrap
// ---------------------------------------------------------------------------

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

// ---------------------------------------------------------------------------
// Test child that reads from ShopContext
// ---------------------------------------------------------------------------

function ShopInfo() {
  const shop = useResolvedShop();
  return (
    <div>
      <span data-testid="shop-id">{shop.id}</span>
      <span data-testid="shop-name">{shop.name}</span>
      <span data-testid="shop-slug">{shop.slug}</span>
    </div>
  );
}

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
  initialPath?: string;
}

function renderResolver(options: WrapperOptions = {}) {
  const { shops = [], loading = false, error = false, initialPath = '/frietjes/nl/gent' } = options;

  (useActiveShops as Mock).mockReturnValue({
    data: shops,
    isLoading: loading,
    isError: error,
  });

  const queryClient = makeQueryClient();

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/:brandSlug/:lang/:shopSlug" element={<ShopResolver />}>
            <Route index element={<ShopInfo />} />
          </Route>
          <Route path="/:brandSlug/:lang/shops" element={<div>ShopChooser</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopResolver', () => {
  it('shows loading state while fetching shops', () => {
    renderResolver({ loading: true });
    expect(screen.getByText(i18next.t('loading'))).toBeInTheDocument();
  });

  it('shows error state when the fetch fails', () => {
    renderResolver({ error: true });
    expect(screen.getByText(i18next.t('error'))).toBeInTheDocument();
  });

  it('redirects to shop chooser when slug is not found', () => {
    // shops list does not contain 'gent'
    renderResolver({ shops: [makeShop({ slug: 'brugge' })], initialPath: '/frietjes/nl/gent' });
    expect(screen.getByText('ShopChooser')).toBeInTheDocument();
  });

  it('provides resolved shop to children when slug matches', () => {
    renderResolver({
      shops: [makeShop({ id: 'shop-42', slug: 'gent', name: 'Frietjes Gent' })],
      initialPath: '/frietjes/nl/gent',
    });
    expect(screen.getByTestId('shop-id')).toHaveTextContent('shop-42');
    expect(screen.getByTestId('shop-name')).toHaveTextContent('Frietjes Gent');
    expect(screen.getByTestId('shop-slug')).toHaveTextContent('gent');
  });

  it('matches slug case-sensitively', () => {
    // 'Gent' !== 'gent' — should not match and redirect to chooser
    renderResolver({
      shops: [makeShop({ slug: 'Gent' })],
      initialPath: '/frietjes/nl/gent',
    });
    expect(screen.getByText('ShopChooser')).toBeInTheDocument();
  });
});
