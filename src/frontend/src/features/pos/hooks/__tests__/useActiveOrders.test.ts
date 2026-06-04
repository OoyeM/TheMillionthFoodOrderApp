import { describe, it, expect, vi, beforeEach } from 'vitest';
import { renderHook, waitFor, act } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { http, HttpResponse } from 'msw';
import type { ReactNode } from 'react';
import { createElement } from 'react';

import { server } from '../../../../test/msw/server';

// Capture the SignalR callback so the test can fire it manually.
let capturedOnStatusChange: ((u: unknown) => void) | undefined;
vi.mock('../../../../api/useOrderUpdates', () => ({
  useOrderUpdates: (opts: { onStatusChange?: (u: unknown) => void }) => {
    capturedOnStatusChange = opts.onStatusChange;
    return { status: 'connected' as const };
  },
}));

import { useActiveOrders } from '../useActiveOrders';

const brandSlug = 'frietjes';
const shopId = '00000000-0000-0000-0000-000000000099';

function makeWrapper(client: QueryClient) {
  return ({ children }: { children: ReactNode }) =>
    createElement(QueryClientProvider, { client }, children);
}

describe('useActiveOrders', () => {
  beforeEach(() => {
    capturedOnStatusChange = undefined;
  });

  it('invalidates the active-orders query when a status change event fires', async () => {
    let callCount = 0;
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () => {
        callCount += 1;
        return HttpResponse.json({ orders: [] });
      }),
    );

    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    const { result } = renderHook(() => useActiveOrders(brandSlug, shopId), {
      wrapper: makeWrapper(client),
    });

    await waitFor(() => { expect(result.current.isLoading).toBe(false); });
    expect(callCount).toBe(1);
    expect(capturedOnStatusChange).toBeDefined();

    // eslint-disable-next-line @typescript-eslint/require-await -- async act() flushes pending effects/microtasks after firing the callback; the sync overload would not
    await act(async () => {
      // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- asserted defined via expect(capturedOnStatusChange).toBeDefined() above
      capturedOnStatusChange!({ orderId: 'x', newStatus: 'Preparing' });
    });

    await waitFor(() => { expect(callCount).toBe(2); });
  });

  it('does not run the query when brandSlug or shopId is empty', async () => {
    let callCount = 0;
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () => {
        callCount += 1;
        return HttpResponse.json({ orders: [] });
      }),
    );

    const client = new QueryClient({
      defaultOptions: { queries: { retry: false } },
    });

    renderHook(() => useActiveOrders('', shopId), {
      wrapper: makeWrapper(client),
    });
    // Give the query a tick — it should still not fire.
    await new Promise((r) => setTimeout(r, 0));
    expect(callCount).toBe(0);
  });
});
