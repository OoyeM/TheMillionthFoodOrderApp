import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { describe, it, expect } from 'vitest';

// Initialize i18n before tests
import '../i18n/config';

/**
 * Smoke test: verifies the app renders without throwing.
 * We bypass the full router (which uses createBrowserRouter and navigate)
 * by rendering components in isolation with MemoryRouter.
 */
describe('App smoke test', () => {
  it('renders the storefront home page without crashing', async () => {
    const { Home } = await import('@features/storefront/pages/Home');
    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    render(
      <QueryClientProvider client={client}>
        <MemoryRouter>
          <Home />
        </MemoryRouter>
      </QueryClientProvider>,
    );

    // Dutch fallback translation should be visible
    expect(screen.getByRole('heading', { name: 'Welkom' })).toBeInTheDocument();
  });

  it('renders the POS dashboard without crashing', async () => {
    const { PosDashboard } = await import('@features/pos/pages/Dashboard');

    render(
      <MemoryRouter>
        <PosDashboard />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'POS Dashboard' })).toBeInTheDocument();
  });

  it('renders the Admin dashboard without crashing', async () => {
    const { AdminDashboard } = await import('@features/admin/pages/Dashboard');

    render(
      <MemoryRouter>
        <AdminDashboard />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Admin Dashboard' })).toBeInTheDocument();
  });
});
