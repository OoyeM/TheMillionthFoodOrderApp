import { type ReactNode } from 'react';
import { render, type RenderOptions, type RenderResult } from '@testing-library/react';
import { MemoryRouter, type MemoryRouterProps } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';

/**
 * Creates a fresh QueryClient with retries disabled (suitable for testing).
 */
export function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });
}

interface RenderWithProvidersOptions extends RenderOptions {
  /** Initial URL entries for MemoryRouter. Defaults to ['/']. */
  initialEntries?: MemoryRouterProps['initialEntries'];
  /** Pre-configured QueryClient. A fresh one is created per call if omitted. */
  queryClient?: QueryClient;
}

/**
 * Wraps the given UI with a fresh QueryClientProvider and MemoryRouter.
 * Use this for any component that relies on TanStack Query or React Router.
 *
 * @example
 * const { getByText } = renderWithProviders(<MyComponent />);
 */
export function renderWithProviders(
  ui: ReactNode,
  {
    initialEntries = ['/'],
    queryClient,
    ...renderOptions
  }: RenderWithProvidersOptions = {},
): RenderResult {
  const client = queryClient ?? createTestQueryClient();

  return render(
    <QueryClientProvider client={client}>
      <MemoryRouter initialEntries={initialEntries}>{ui}</MemoryRouter>
    </QueryClientProvider>,
    renderOptions,
  );
}
