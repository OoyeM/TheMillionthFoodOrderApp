import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
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
});
