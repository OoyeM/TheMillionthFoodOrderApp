import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import '../../../../i18n/config';

// The SignalR hub is exercised end-to-end elsewhere — for the kitchen page we mock
// it so the test never touches a real WebSocket. Keep this mock at module-top so it
// applies before the component imports it.
vi.mock('../../../../api/useOrderUpdates', () => ({
  useOrderUpdates: () => ({ status: 'connected' as const }),
}));

import { KitchenDisplay } from '../KitchenDisplay';

const brandSlug = 'frietjes';
const shopId = '00000000-0000-0000-0000-000000000001';

function makeOrder(overrides: Partial<{
  id: string;
  orderNumber: string;
  orderType: 'Pickup' | 'EatIn' | 'Delivery';
  tableNumber: string | null;
  customerName: string | null;
  items: Array<{ productName: string; quantity: number; modifiers: Array<{ name: string }> }>;
}>) {
  const items = overrides.items ?? [
    { productName: 'Frietje', quantity: 1, modifiers: [] },
  ];
  return {
    id: overrides.id ?? 'order-1',
    orderNumber: overrides.orderNumber ?? '0001',
    shopId,
    brandSlug,
    orderType: overrides.orderType ?? 'Pickup',
    statusName: 'Placed',
    customerName: overrides.customerName ?? null,
    items: items.map((it, idx) => ({
      productId: `prod-${idx}`,
      productName: it.productName,
      quantity: it.quantity,
      unitGrossPrice: 3.5,
      unitNetPrice: 3.3,
      unitVatAmount: 0.2,
      lineTotal: 3.5 * it.quantity,
      selectedModifiers: it.modifiers.map((m, mIdx) => ({
        modifierId: `mod-${idx}-${mIdx}`,
        modifierName: m.name,
        priceAdjustment: 0,
      })),
    })),
    vatRatePercent: 6,
    subtotalGross: 3.5,
    totalVatAmount: 0.2,
    totalNet: 3.3,
    totalGross: 3.5,
    createdAt: '2026-05-13T12:00:00Z',
    paymentMethod: 'CashAtPickup',
    tableNumber: overrides.tableNumber ?? null,
  };
}

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/pos/shops/:shopId/kitchen"
        element={<KitchenDisplay />}
      />
    </Routes>,
    { initialEntries: [`/${brandSlug}/nl/pos/shops/${shopId}/kitchen`] },
  );
}

describe('KitchenDisplay', () => {
  beforeEach(() => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [] }),
      ),
    );
  });

  it('shows the empty state when there are no active orders', async () => {
    renderPage();
    expect(await screen.findByTestId('kitchen-empty')).toBeInTheDocument();
  });

  it('renders order cards in the order returned by the API', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({
          orders: [
            makeOrder({ id: 'a', orderNumber: '0001', customerName: 'Alice' }),
            makeOrder({ id: 'b', orderNumber: '0002', customerName: 'Bob' }),
            makeOrder({ id: 'c', orderNumber: '0003', customerName: 'Carla' }),
          ],
        }),
      ),
    );

    renderPage();

    const cards = await screen.findAllByTestId('kitchen-order-card');
    expect(cards).toHaveLength(3);
    expect(within(cards[0]!).getByText('#0001')).toBeInTheDocument();
    expect(within(cards[1]!).getByText('#0002')).toBeInTheDocument();
    expect(within(cards[2]!).getByText('#0003')).toBeInTheDocument();
  });

  it('renders each item with its modifiers', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({
          orders: [
            makeOrder({
              id: 'a',
              orderNumber: '0010',
              items: [
                {
                  productName: 'Frietje Speciaal',
                  quantity: 2,
                  modifiers: [{ name: 'Extra mayo' }, { name: 'Geen ui' }],
                },
              ],
            }),
          ],
        }),
      ),
    );

    renderPage();

    expect(await screen.findByText('Frietje Speciaal')).toBeInTheDocument();
    expect(screen.getByText('2×')).toBeInTheDocument();
    expect(screen.getByText('+ Extra mayo')).toBeInTheDocument();
    expect(screen.getByText('+ Geen ui')).toBeInTheDocument();
  });

  it('renders the table number badge only when present', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({
          orders: [
            makeOrder({ id: 'a', orderNumber: '0021', orderType: 'EatIn', tableNumber: '5' }),
            makeOrder({ id: 'b', orderNumber: '0022', orderType: 'Pickup', tableNumber: null }),
          ],
        }),
      ),
    );

    renderPage();

    await waitFor(() =>
      expect(screen.getAllByTestId('kitchen-order-card')).toHaveLength(2),
    );

    const tableBadges = screen.queryAllByTestId('kitchen-order-table');
    expect(tableBadges).toHaveLength(1);
    expect(tableBadges[0]).toHaveTextContent('5');
  });

  it('renders the connection status indicator', async () => {
    renderPage();
    expect(await screen.findByTestId('kitchen-connection-status')).toBeInTheDocument();
  });

  it('renders an advance button per allowed next status and posts the transition on tap', async () => {
    const advanceCalls: Array<{ orderId: string; toStatusId: string }> = [];
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({
          orders: [makeOrder({ id: 'a', orderNumber: '0001' })],
        }),
      ),
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/order-lifecycle`, () =>
        HttpResponse.json({
          shopId,
          statuses: [
            { id: 's-placed', name: 'Placed', systemKey: 'placed', sortOrder: 0, isEnabled: true, isTerminal: false, colorHex: null },
            { id: 's-prep', name: 'Preparing', systemKey: 'preparing', sortOrder: 1, isEnabled: true, isTerminal: false, colorHex: '#f59e0b' },
          ],
          transitions: [{ id: 't1', fromStatusId: 's-placed', toStatusId: 's-prep' }],
        }),
      ),
      http.post(
        `/api/brands/${brandSlug}/shops/${shopId}/orders/:orderId/status`,
        async ({ params, request }) => {
          const body = (await request.json()) as { toStatusId: string };
          advanceCalls.push({ orderId: String(params.orderId), toStatusId: body.toStatusId });
          return HttpResponse.json({
            ...makeOrder({ id: 'a', orderNumber: '0001' }),
            statusName: 'Preparing',
          });
        },
      ),
    );

    const user = userEvent.setup();
    renderPage();

    const advanceBtn = await screen.findByTestId('kitchen-advance-button');
    expect(advanceBtn).toHaveTextContent('Preparing');

    await user.click(advanceBtn);

    await waitFor(() => expect(advanceCalls).toHaveLength(1));
    expect(advanceCalls[0]).toEqual({ orderId: 'a', toStatusId: 's-prep' });
  });
});
