import { describe, it, expect, vi, afterEach } from 'vitest';
import { render, screen, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { BffAuthProvider } from '../BffAuthProvider';
import { useAuth } from '../useAuth';
import * as useSessionKeepaliveModule from '../useSessionKeepalive';

// bffClient uses baseURL '/bff' which resolves to /bff in jsdom

/**
 * Helper component that shows auth state.
 */
function AuthDisplay() {
  const { user, isAuthenticated, isLoading } = useAuth();
  if (isLoading) return <div data-testid="loading">Loading...</div>;
  return (
    <div>
      <span data-testid="is-authenticated">{String(isAuthenticated)}</span>
      <span data-testid="display-name">{user?.displayName ?? 'null'}</span>
    </div>
  );
}

function makeQueryClient() {
  return new QueryClient({ defaultOptions: { queries: { retry: false } } });
}

/**
 * Tests for src/auth/BffAuthProvider.tsx
 *
 * - Fetches /bff/user via TanStack Query on mount
 * - auth:session-expired event invalidates the user query
 * - useSessionKeepalive is invoked on mount
 */
describe('BffAuthProvider', () => {
  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('shows loading state while /bff/user is being fetched', () => {
    // Make /bff/user hang for a moment
    server.use(
      http.get('/bff/user', async () => {
        await new Promise((r) => setTimeout(r, 200));
        return HttpResponse.json({ isAuthenticated: false });
      }),
    );

    render(
      <QueryClientProvider client={makeQueryClient()}>
        <BffAuthProvider>
          <AuthDisplay />
        </BffAuthProvider>
      </QueryClientProvider>,
    );

    expect(screen.getByTestId('loading')).toBeInTheDocument();
  });

  it('renders authenticated state after /bff/user resolves', async () => {
    render(
      <QueryClientProvider client={makeQueryClient()}>
        <BffAuthProvider>
          <AuthDisplay />
        </BffAuthProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });
    expect(screen.getByTestId('display-name').textContent).toBe('Test User');
  });

  it('renders unauthenticated state when /bff/user returns isAuthenticated=false', async () => {
    server.use(
      http.get('/bff/user', () => HttpResponse.json({ isAuthenticated: false })),
    );

    render(
      <QueryClientProvider client={makeQueryClient()}>
        <BffAuthProvider>
          <AuthDisplay />
        </BffAuthProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
    });
  });

  it('invalidates user query when auth:session-expired event fires', async () => {
    // First render with authenticated user
    render(
      <QueryClientProvider client={makeQueryClient()}>
        <BffAuthProvider>
          <AuthDisplay />
        </BffAuthProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('true');
    });

    // Switch server to return unauthenticated
    server.use(
      http.get('/bff/user', () => HttpResponse.json({ isAuthenticated: false })),
    );

    // Dispatch the session-expired event
    window.dispatchEvent(new CustomEvent('auth:session-expired'));

    await waitFor(() => {
      expect(screen.getByTestId('is-authenticated').textContent).toBe('false');
    });
  });

  it('calls useSessionKeepalive on mount', async () => {
    const keepaliveSpy = vi
      .spyOn(useSessionKeepaliveModule, 'useSessionKeepalive')
      .mockImplementation(() => undefined);

    render(
      <QueryClientProvider client={makeQueryClient()}>
        <BffAuthProvider>
          <div />
        </BffAuthProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(keepaliveSpy).toHaveBeenCalled();
    });
  });
});
