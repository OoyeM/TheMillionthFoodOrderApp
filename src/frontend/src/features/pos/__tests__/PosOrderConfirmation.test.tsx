/**
 * t-15 — PosOrderConfirmation page: displays order number, back-to-menu navigation [AC5]
 *
 * Covers:
 * - Renders order number from route params
 * - Back button navigates to /pos dashboard
 * - Missing orderNumber param redirects to /pos
 */
// Import i18n before any component to ensure translations are initialised
import '@/i18n/config';

import { describe, it, expect } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { render } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { PosOrderConfirmationInner, PosOrderConfirmation } from '../pages/PosOrderConfirmation';

// ── Helpers ──────────────────────────────────────────────────────────────────

function createTestClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
}

/**
 * Renders PosOrderConfirmationInner directly with controlled props.
 */
function renderInner(orderNumber: string, onBackToMenu = () => { /* no-op */ }) {
  return render(
    <QueryClientProvider client={createTestClient()}>
      <MemoryRouter>
        <PosOrderConfirmationInner orderNumber={orderNumber} onBackToMenu={onBackToMenu} />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('PosOrderConfirmationInner (t-15)', () => {
  it('renders the order number in the confirmation element', () => {
    renderInner('ORD-042');

    expect(screen.getByTestId('pos-order-number')).toBeInTheDocument();
    // The translated text contains the order number (interpolated via i18n)
    expect(screen.getByTestId('pos-order-number').textContent).toContain('ORD-042');
  });

  it('renders a heading for the confirmation title', () => {
    renderInner('ORD-001');

    // h1 heading exists — exact text depends on active language (NL fallback: "Bestelling geplaatst!")
    expect(screen.getByRole('heading', { level: 1 })).toBeInTheDocument();
  });

  it('calls onBackToMenu when the back button is clicked', () => {
    let backCalled = false;
    renderInner('ORD-001', () => { backCalled = true; });

    const backBtn = screen.getByRole('button');
    fireEvent.click(backBtn);

    expect(backCalled).toBe(true);
  });
});

describe('PosOrderConfirmation route component (t-15)', () => {
  it('renders order number from route param', async () => {
    render(
      <QueryClientProvider client={createTestClient()}>
        <MemoryRouter initialEntries={['/frietjes/nl/pos/confirmation/ORD-099']}>
          <Routes>
            <Route path="/:brandSlug/:lang/pos/confirmation/:orderNumber" element={<PosOrderConfirmation />} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('pos-order-number')).toBeInTheDocument();
      expect(screen.getByTestId('pos-order-number').textContent).toContain('ORD-099');
    });
  });

  it('back button navigates to the /pos route', async () => {
    render(
      <QueryClientProvider client={createTestClient()}>
        <MemoryRouter initialEntries={['/frietjes/nl/pos/confirmation/ORD-099']}>
          <Routes>
            <Route path="/:brandSlug/:lang/pos/confirmation/:orderNumber" element={<PosOrderConfirmation />} />
            <Route path="/:brandSlug/:lang/pos" element={<div data-testid="pos-dashboard">POS Dashboard</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('pos-order-number')).toBeInTheDocument();
    });

    const backBtn = screen.getByRole('button');
    fireEvent.click(backBtn);

    await waitFor(() => {
      expect(screen.getByTestId('pos-dashboard')).toBeInTheDocument();
    });
  });

  it('redirects to /pos when orderNumber param is missing', async () => {
    render(
      <QueryClientProvider client={createTestClient()}>
        <MemoryRouter initialEntries={['/frietjes/nl/pos/confirmation']}>
          <Routes>
            {/*
             * Intentionally omit :orderNumber to simulate a direct navigation
             * to /confirmation without an order number — component should redirect.
             */}
            <Route path="/:brandSlug/:lang/pos/confirmation" element={<PosOrderConfirmation />} />
            <Route path="/:brandSlug/:lang/pos" element={<div data-testid="pos-dashboard">POS Dashboard</div>} />
          </Routes>
        </MemoryRouter>
      </QueryClientProvider>,
    );

    // useEffect fires after mount; navigate({ replace: true }) brings us to /pos
    await waitFor(() => {
      expect(screen.getByTestId('pos-dashboard')).toBeInTheDocument();
    });
  });
});
