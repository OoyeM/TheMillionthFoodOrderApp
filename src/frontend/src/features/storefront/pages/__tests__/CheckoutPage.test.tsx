/**
 * Tests for CheckoutPage:
 * (a) Guest submit is blocked without first/last/email/phone (US-FP-051).
 * (b) Logged-in user prefills first/last/email/phone from profile (US-FP-051).
 * (c) Submitted body includes languageCode and customerFirstName/customerLastName (US-FP-051).
 * (d) Time-slot picker scenarios (US-FP-019).
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

  // (d) Time-slot picker (US-FP-019)
  describe('time-slot picker', () => {
    // Fixture: two slots — first available, second full.
    const slot1Start = '2026-06-10T08:00:00Z';
    const slot2Start = '2026-06-10T08:15:00Z';

    function overrideTimeSlotsEnabled() {
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/time-slots', () =>
          HttpResponse.json({
            isEnabled: true,
            intervalMinutes: 15,
            slots: [
              { slotStart: slot1Start, label: '10:00', isAvailable: true },
              { slotStart: slot2Start, label: '10:15', isAvailable: false },
            ],
            activeOrderCount: null,
          }),
        ),
      );
    }

    it('renders ASAP option and slot labels when slots are enabled; ASAP is pre-selected', async () => {
      overrideTimeSlotsEnabled();

      renderCheckoutPage({ authCtx: makeGuestContext() });

      // Wait for slot picker to appear
      await screen.findByText(/zo snel mogelijk/i);
      expect(screen.getByText('10:00')).toBeInTheDocument();
      expect(screen.getByText('10:15')).toBeInTheDocument();

      // ASAP radio should be checked
      const asapRadio = screen.getByDisplayValue('asap');
      expect(asapRadio).toBeChecked();
    });

    it('renders the full slot disabled and clicking it does not change selection (AC2)', async () => {
      overrideTimeSlotsEnabled();

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByText(/zo snel mogelijk/i);

      // The full slot radio (10:15) should be disabled
      const fullSlotRadio = screen.getByDisplayValue(slot2Start);
      expect(fullSlotRadio).toBeDisabled();

      // Clicking the full slot leaves ASAP selected
      fireEvent.click(fullSlotRadio);
      expect(screen.getByDisplayValue('asap')).toBeChecked();
      expect(fullSlotRadio).not.toBeChecked();

      // The suffix "(volzet)" should be visible
      expect(screen.getByText(/volzet/i)).toBeInTheDocument();
    });

    it('renders the slot picker for EatIn order type as well (decision 2)', async () => {
      overrideTimeSlotsEnabled();

      renderCheckoutPage({ authCtx: makeGuestContext() });

      // Switch to EatIn
      await screen.findByText(/zo snel mogelijk/i);
      fireEvent.click(screen.getByDisplayValue('EatIn'));

      // Picker still present
      expect(screen.getByText(/zo snel mogelijk/i)).toBeInTheDocument();
      expect(screen.getByText('10:00')).toBeInTheDocument();
    });

    it('sends timeSlotStart ISO string when a slot is selected', async () => {
      overrideTimeSlotsEnabled();

      let capturedBody: Record<string, unknown> | null = null;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-slot-1',
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
              createdAt: '2026-06-10T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByText(/zo snel mogelijk/i);

      // Select the available slot
      fireEvent.click(screen.getByDisplayValue(slot1Start));

      // Fill required fields
      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      // Select Pickup + CashAtPickup (indices shift by 1 ASAP radio, so all 3 order types + ASAP + slots)
      // Use getByDisplayValue for type-safety instead of positional index
      fireEvent.click(screen.getByDisplayValue('Pickup'));
      fireEvent.click(screen.getByDisplayValue('CashAtPickup'));

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => { expect(capturedBody).not.toBeNull(); });
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees non-null
      expect(capturedBody!.timeSlotStart).toBe(slot1Start);
    });

    it('sends timeSlotStart null when ASAP is selected', async () => {
      overrideTimeSlotsEnabled();

      let capturedBody: Record<string, unknown> | null = null;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-asap-1',
              orderNumber: 'ORD-002',
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
              createdAt: '2026-06-10T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByText(/zo snel mogelijk/i);

      // ASAP stays selected (default)

      // Fill required fields
      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      fireEvent.click(screen.getByDisplayValue('Pickup'));
      fireEvent.click(screen.getByDisplayValue('CashAtPickup'));

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => { expect(capturedBody).not.toBeNull(); });
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees non-null
      expect(capturedBody!.timeSlotStart).toBeNull();
    });

    it('shows no picker and shows queue notice with count when slots are disabled', async () => {
      // Default handler returns activeOrderCount: 0 — override to test non-zero
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/time-slots', () =>
          HttpResponse.json({
            isEnabled: false,
            intervalMinutes: null,
            slots: [],
            activeOrderCount: 3,
          }),
        ),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      // Wait for the form to render
      await screen.findByRole('button', { name: /bestelling plaatsen/i });

      // No time-slot picker legend present
      expect(screen.queryByText(/tijdslot/i)).not.toBeInTheDocument();

      // Queue notice present with count — use a regex that matches the full phrase
      await waitFor(() => {
        expect(screen.getByText(/bestelling.*voor jou/i)).toBeInTheDocument();
      });
    });

    it('shows queueEmpty message when activeOrderCount is 0', async () => {
      // Default handler already returns activeOrderCount: 0
      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByRole('button', { name: /bestelling plaatsen/i });

      await waitFor(() => {
        expect(screen.getByText(/direct naar de keuken/i)).toBeInTheDocument();
      });
    });

    it('shows no picker and no queue notice when slots query errors; checkout still works as ASAP', async () => {
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/time-slots', () =>
          HttpResponse.json({ error: 'server error' }, { status: 500 }),
        ),
      );

      let capturedBody: Record<string, unknown> | null = null;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-err-1',
              orderNumber: 'ORD-003',
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
              createdAt: '2026-06-10T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByRole('button', { name: /bestelling plaatsen/i });

      // Neither picker nor notice should be shown
      expect(screen.queryByText(/zo snel mogelijk/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/direct naar de keuken/i)).not.toBeInTheDocument();
      expect(screen.queryByText(/bestelling.*voor jou/i)).not.toBeInTheDocument();

      // Ordering degrades to ASAP: submit succeeds with timeSlotStart null
      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      fireEvent.click(screen.getByDisplayValue('Pickup'));
      fireEvent.click(screen.getByDisplayValue('CashAtPickup'));

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => { expect(capturedBody).not.toBeNull(); });
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees non-null
      expect(capturedBody!.timeSlotStart).toBeNull();
    });

    it('resets a selected slot to ASAP when slots are disabled mid-session', async () => {
      overrideTimeSlotsEnabled();

      let postCalls = 0;
      let capturedBody: Record<string, unknown> | null = null;
      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          postCalls += 1;
          if (postCalls === 1) {
            // First submit: the slot just filled up — triggers a slots refetch client-side
            return HttpResponse.json(
              { errors: { timeSlotStart: ['The selected time slot is full.'] } },
              { status: 400 },
            );
          }
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              id: 'order-reset-1',
              orderNumber: 'ORD-004',
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
              createdAt: '2026-06-10T10:00:00Z',
              paymentMethod: 'CashAtPickup',
            },
            { status: 201 },
          );
        }),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByText(/zo snel mogelijk/i);

      // Select the available slot while slots are still enabled
      fireEvent.click(screen.getByDisplayValue(slot1Start));
      expect(screen.getByDisplayValue(slot1Start)).toBeChecked();

      // Admin disables time-slot ordering mid-session: subsequent fetches return disabled
      server.use(
        http.get('/api/brands/:slug/shops/:shopId/time-slots', () =>
          HttpResponse.json({
            isEnabled: false,
            intervalMinutes: null,
            slots: [],
            activeOrderCount: 0,
          }),
        ),
      );

      // Fill required fields and submit — the 400 triggers the slots refetch
      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      fireEvent.click(screen.getByDisplayValue('Pickup'));
      fireEvent.click(screen.getByDisplayValue('CashAtPickup'));

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      // The refetch returns isEnabled:false → the picker unmounts
      await waitFor(() => {
        expect(screen.queryByText(/zo snel mogelijk/i)).not.toBeInTheDocument();
      });

      // The second submit must carry timeSlotStart null — not the stale ISO string,
      // which would 400 forever with no control left on screen to clear it.
      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      await waitFor(() => { expect(capturedBody).not.toBeNull(); });
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above guarantees non-null
      expect(capturedBody!.timeSlotStart).toBeNull();
    });

    it('shows slot-full message and preserves cart when server returns 400 errors.timeSlotStart', async () => {
      overrideTimeSlotsEnabled();

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', () =>
          HttpResponse.json(
            { errors: { timeSlotStart: ['The selected time slot is full.'] } },
            { status: 400 },
          ),
        ),
      );

      renderCheckoutPage({ authCtx: makeGuestContext() });

      await screen.findByText(/zo snel mogelijk/i);

      // Fill required fields
      fireEvent.change(screen.getByLabelText(/voornaam/i), { target: { value: 'Jane' } });
      fireEvent.change(screen.getByLabelText(/achternaam/i), { target: { value: 'Doe' } });
      fireEvent.change(screen.getByLabelText(/e-mailadres/i), { target: { value: 'jane@example.com' } });
      fireEvent.change(screen.getByLabelText(/telefoonnummer/i), { target: { value: '+32470000001' } });

      fireEvent.click(screen.getByDisplayValue('Pickup'));
      fireEvent.click(screen.getByDisplayValue('CashAtPickup'));

      fireEvent.click(screen.getByRole('button', { name: /bestelling plaatsen/i }));

      // Slot-full message should appear
      await waitFor(() => {
        expect(screen.getByText(/tijdslot is net volgeboekt/i)).toBeInTheDocument();
      });

      // Cart is preserved — cart items still in localStorage
      const cartKey = 'cart:frietjes:shop-1';
      const cart = JSON.parse(localStorage.getItem(cartKey) ?? '{}') as { items: unknown[] };
      expect(cart.items.length).toBeGreaterThan(0);
    });
  });
});
