/**
 * Tests for CheckoutPage (US-FP-051):
 * (a) Guest submit is blocked without first/last/email/phone.
 * (b) Logged-in user prefills first/last/email/phone from profile.
 * (c) Submitted body includes languageCode and customerFirstName/customerLastName.
 */
import { beforeAll, describe, it, expect, vi, type Mock } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/msw/server';
import { AuthContext, type AuthContextValue } from '@/auth/AuthContext';
import { ShopContext } from '../../context/shopContextValue';
import type { ResolvedShop } from '../../context/shopContextValue';
import type { AuthUser } from '@/types/auth';
import '@/i18n/config';
import i18next from 'i18next';

// ---------------------------------------------------------------------------
// Module-level mocks — must be hoisted before the component import
// ---------------------------------------------------------------------------

vi.mock('../../hooks/useResolvedShop', () => ({
  useResolvedShop: vi.fn(),
}));

// Import after mock registration
import { useResolvedShop } from '../../hooks/useResolvedShop';
import { CheckoutPage } from '../CheckoutPage';

// ---------------------------------------------------------------------------
// i18n bootstrap
// ---------------------------------------------------------------------------

beforeAll(async () => {
  await i18next.changeLanguage('nl');
});

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function makeQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false }, mutations: { retry: false } } });
}

/** Default resolved shop used by most tests. */
const defaultShop: ResolvedShop = {
  id: 'shop-1',
  name: 'Gent Centrum',
  slug: 'gent-centrum',
  isOpen: true,
  eatIn: { isEnabled: true, requiresTableNumber: true },
  timeSlotOrdering: { isEnabled: false, intervalMinutes: null, maxOrdersPerInterval: null },
};

/** Unauthenticated context (guest). */
function makeGuestContext(): AuthContextValue {
  return {
    isAuthenticated: false,
    user: null,
    isLoading: false,
    login: () => undefined,
    logout: () => Promise.resolve(),
    hasRole: () => false,
    hasAnyRole: () => false,
  };
}

/** Authenticated customer context with full profile. */
function makeCustomerContext(overrides: Partial<AuthUser> = {}): AuthContextValue {
  const user: AuthUser = {
    userId: 'cust-1',
    displayName: 'Test Customer',
    email: 'test.customer@example.com',
    roles: ['customer'],
    brandSlug: null,
    firstName: 'Test',
    lastName: 'Customer',
    phoneNumber: '+32470000099',
    ...overrides,
  };
  return {
    isAuthenticated: true,
    user,
    isLoading: false,
    login: () => undefined,
    logout: () => Promise.resolve(),
    hasRole: (role) => user.roles.includes(role),
    hasAnyRole: (roles) => roles.some((r) => user.roles.includes(r)),
  };
}

interface RenderOptions {
  authCtx?: AuthContextValue;
  shop?: ResolvedShop;
  lang?: string;
}

/**
 * Renders CheckoutPage inside a full provider stack.
 * Prefills localStorage cart so the component renders rather than redirecting to menu.
 */
function renderCheckoutPage({
  authCtx = makeGuestContext(),
  shop = defaultShop,
  lang = 'nl',
}: RenderOptions = {}) {
  // Prefill localStorage with a cart item so the page doesn't immediately redirect
  const cartKey = `cart:frietjes:${shop.id}`;
  const cartValue = {
    brandSlug: 'frietjes',
    shopId: shop.id,
    items: [
      {
        productId: 'prod-1',
        productName: 'Kleine friet',
        quantity: 1,
        unitGrossPrice: 3.5,
        selectedModifiers: [],
      },
    ],
  };
  localStorage.setItem(cartKey, JSON.stringify(cartValue));

  (useResolvedShop as Mock).mockReturnValue(shop);

  const queryClient = makeQueryClient();

  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[`/frietjes/${lang}/gent-centrum/checkout`]}>
        <Routes>
          <Route
            path="/:brandSlug/:lang/:shopSlug/checkout"
            element={
              <AuthContext.Provider value={authCtx}>
                <ShopContext.Provider value={shop}>
                  <CheckoutPage />
                </ShopContext.Provider>
              </AuthContext.Provider>
            }
          />
          <Route
            path="/:brandSlug/:lang/:shopSlug/menu"
            element={<div data-testid="menu-page">MenuPage</div>}
          />
          <Route
            path="/:brandSlug/:lang/:shopSlug/order/:orderId"
            element={<div data-testid="confirmation-page">Confirmation</div>}
          />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('CheckoutPage (US-FP-051)', () => {
  // (a) Guest: submit blocked when required fields are empty
  describe('guest checkout validation', () => {
    it('does not submit when first name is missing', async () => {
      let submitted = false;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', () => {
          submitted = true;
          return HttpResponse.json({}, { status: 201 });
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      // Wait for form to appear
      await screen.findByLabelText(/achternaam/i);

      // Fill last name / email / phone but leave first name empty
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // Select order type and payment method
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!); // Pickup
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!); // CashAtPickup

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      // API must NOT have been called
      await waitFor(() => {
        expect(submitted).toBe(false);
      });
    });

    it('does not submit when last name is missing', async () => {
      let submitted = false;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', () => {
          submitted = true;
          return HttpResponse.json({}, { status: 201 });
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByLabelText(/voornaam/i);

      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      // leave last name empty
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!);
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!);
      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => {
        expect(submitted).toBe(false);
      });
    });

    it('does not submit when email is missing', async () => {
      let submitted = false;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', () => {
          submitted = true;
          return HttpResponse.json({}, { status: 201 });
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByLabelText(/voornaam/i);

      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      // leave email empty
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!);
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!);
      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => {
        expect(submitted).toBe(false);
      });
    });

    it('does not submit when phone is missing', async () => {
      let submitted = false;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', () => {
          submitted = true;
          return HttpResponse.json({}, { status: 201 });
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByLabelText(/voornaam/i);

      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      // leave phone empty

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!);
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!);
      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => {
        expect(submitted).toBe(false);
      });
    });
  });

  // (b) Logged-in: profile fields prefilled
  describe('authenticated checkout — profile prefill', () => {
    it('prefills first name, last name, email, and phone from profile', async () => {
      const ctx = makeCustomerContext();
      renderCheckoutPage({ authCtx: ctx });

      // Form should render with prefilled values
      const firstNameInput = await screen.findByDisplayValue('Test');
      expect(firstNameInput).toBeInTheDocument();

      expect(screen.getByDisplayValue('Customer')).toBeInTheDocument();
      expect(screen.getByDisplayValue('test.customer@example.com')).toBeInTheDocument();
      expect(screen.getByDisplayValue('+32470000099')).toBeInTheDocument();
    });

    it('renders first name and last name fields as disabled for authenticated users', async () => {
      const ctx = makeCustomerContext();
      renderCheckoutPage({ authCtx: ctx });

      const firstNameInput = await screen.findByDisplayValue('Test');
      expect(firstNameInput).toBeDisabled();

      const lastNameInput = screen.getByDisplayValue('Customer');
      expect(lastNameInput).toBeDisabled();
    });
  });

  // (c) Submitted body includes languageCode and split name fields
  describe('submitted payload shape', () => {
    it('sends customerFirstName, customerLastName, and languageCode in the request body', async () => {
      let capturedBody: Record<string, unknown> | null = null;

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-123',
              orderNumber: 'ORD-001',
              shopId: 'shop-1',
              brandSlug: 'frietjes',
              orderType: 'Pickup',
              statusName: 'New',
              customerName: 'Jane Doe',
              customerFirstName: 'Jane',
              customerLastName: 'Doe',
              languageCode: 'nl',
              items: [],
              vatRatePercent: 6,
              subtotalGross: 3.5,
              totalVatAmount: 0.2,
              totalNet: 3.3,
              totalGross: 3.5,
              createdAt: '2024-06-01T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext(), lang: 'nl' });

      await screen.findByLabelText(/voornaam/i);

      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // Select Pickup and CashAtPickup
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!);
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!);

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => {
        expect(capturedBody).not.toBeNull();
      });

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees capturedBody is non-null here
      const body = capturedBody!;
      expect(body.customerFirstName).toBe('Jane');
      expect(body.customerLastName).toBe('Doe');
      expect(body.languageCode).toBe('nl');
      // Old field must not be present in request
      expect('customerName' in body).toBe(false);
    });

    it('normalises unsupported lang to nl in languageCode', async () => {
      let capturedBody: Record<string, unknown> | null = null;

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-456',
              orderNumber: 'ORD-002',
              shopId: 'shop-1',
              brandSlug: 'frietjes',
              orderType: 'Pickup',
              statusName: 'New',
              customerName: 'Jane Doe',
              items: [],
              vatRatePercent: 6,
              subtotalGross: 3.5,
              totalVatAmount: 0.2,
              totalNet: 3.3,
              totalGross: 3.5,
              createdAt: '2024-06-01T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      // Route lang = 'en' (unsupported) → should normalise to 'nl'
      renderCheckoutPage({ authCtx: makeGuestContext(), lang: 'en' });

      await screen.findByLabelText(/voornaam/i);

      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[0]!);
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- fixed set of radios always rendered by the form
      fireEvent.click(screen.getAllByRole('radio')[3]!);
      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => {
        expect(capturedBody).not.toBeNull();
      });

      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees capturedBody is non-null here
      expect(capturedBody!.languageCode).toBe('nl');
    });
  });
});
