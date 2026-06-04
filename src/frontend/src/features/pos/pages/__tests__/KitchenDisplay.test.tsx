import { describe, it, expect, vi, beforeEach } from 'vitest';
import { screen, waitFor, within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import '../../../../i18n/config';

// The SignalR hub is exercised end-to-end elsewhere — for the kitchen page we mock
// it so the test never touches a real WebSocket. We capture the onStatusChange
// callback so a test can simulate a new order arriving over the hub.
let triggerStatusChange: (() => void) | undefined;
vi.mock('../../../../api/useOrderUpdates', () => ({
  useOrderUpdates: (opts: { onStatusChange?: () => void }) => {
    triggerStatusChange = opts.onStatusChange;
    return { status: 'connected' as const };
  },
}));

// printTicket drives a real iframe + window.print(), neither of which exist in
// jsdom — mock it so we can assert *what* would print without side effects.
vi.mock('../../utils/printTicket', () => ({ printTicket: vi.fn() }));

// Sound + push utils touch the Web Audio / Notification APIs (absent in jsdom).
// Mock them so we can assert each channel fires without real side effects.
vi.mock('../../utils/playNewOrderSound', () => ({
  playNewOrderSound: vi.fn(),
  primeAudioAlerts: vi.fn(),
}));
vi.mock('../../utils/notifyNewOrder', () => ({
  showNewOrderNotification: vi.fn(),
  requestNotificationPermission: vi.fn().mockResolvedValue('granted'),
  notificationPermission: () => 'default',
}));

import { KitchenDisplay } from '../KitchenDisplay';
import { printTicket } from '../../utils/printTicket';
import { playNewOrderSound, primeAudioAlerts } from '../../utils/playNewOrderSound';
import { showNewOrderNotification, requestNotificationPermission } from '../../utils/notifyNewOrder';

const printTicketMock = vi.mocked(printTicket);
const playSoundMock = vi.mocked(playNewOrderSound);
const primeAudioMock = vi.mocked(primeAudioAlerts);
const showNotificationMock = vi.mocked(showNewOrderNotification);
const requestPermissionMock = vi.mocked(requestNotificationPermission);

const brandSlug = 'frietjes';
const shopId = '00000000-0000-0000-0000-000000000001';

function shopResponse(
  channels: Partial<{
    kitchenDisplayEnabled: boolean;
    ticketPrinterEnabled: boolean;
    pushNotificationEnabled: boolean;
    soundAlertEnabled: boolean;
  }> = {},
) {
  return {
    id: shopId,
    name: 'Frietjes Gent',
    slug: 'frietjes-gent',
    address: { street: 'Veldstraat', number: '42', city: 'Gent', postalCode: '9000', country: 'BE' },
    contactEmail: 'gent@frietjes.be',
    contactPhone: null,
    isActive: true,
    kitchenDisplayEnabled: channels.kitchenDisplayEnabled ?? false,
    ticketPrinterEnabled: channels.ticketPrinterEnabled ?? false,
    pushNotificationEnabled: channels.pushNotificationEnabled ?? false,
    soundAlertEnabled: channels.soundAlertEnabled ?? false,
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-01T00:00:00Z',
  };
}

function makeOrder(overrides: Partial<{
  id: string;
  orderNumber: string;
  orderType: 'Pickup' | 'EatIn' | 'Delivery';
  tableNumber: string | null;
  customerName: string | null;
  items: { productName: string; quantity: number; modifiers: { name: string }[] }[];
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
      productId: `prod-${String(idx)}`,
      productName: it.productName,
      quantity: it.quantity,
      unitGrossPrice: 3.5,
      unitNetPrice: 3.3,
      unitVatAmount: 0.2,
      lineTotal: 3.5 * it.quantity,
      selectedModifiers: it.modifiers.map((m, mIdx) => ({
        modifierId: `mod-${String(idx)}-${String(mIdx)}`,
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
    triggerStatusChange = undefined;
    printTicketMock.mockClear();
    playSoundMock.mockClear();
    primeAudioMock.mockClear();
    showNotificationMock.mockClear();
    requestPermissionMock.mockClear();
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [] }),
      ),
      // All notification channels off by default so most tests never trigger one.
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse()),
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
    /* eslint-disable @typescript-eslint/no-non-null-assertion -- exactly three cards guaranteed by toHaveLength(3) above */
    expect(within(cards[0]!).getByText('#0001')).toBeInTheDocument();
    expect(within(cards[1]!).getByText('#0002')).toBeInTheDocument();
    expect(within(cards[2]!).getByText('#0003')).toBeInTheDocument();
    /* eslint-enable @typescript-eslint/no-non-null-assertion */
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
      { expect(screen.getAllByTestId('kitchen-order-card')).toHaveLength(2); },
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
    const advanceCalls: { orderId: string; toStatusId: string }[] = [];
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

    await waitFor(() => { expect(advanceCalls).toHaveLength(1); });
    expect(advanceCalls[0]).toEqual({ orderId: 'a', toStatusId: 's-prep' });
  });

  it('reprints a ticket when the print button on a card is tapped', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'a', orderNumber: '0001' })] }),
      ),
    );

    const user = userEvent.setup();
    renderPage();

    const reprintBtn = await screen.findByTestId('kitchen-reprint-button');
    await user.click(reprintBtn);

    expect(printTicketMock).toHaveBeenCalledTimes(1);
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- first call exists per toHaveBeenCalledTimes(1) above
    expect(printTicketMock.mock.calls[0]![0]).toMatchObject({ id: 'a', orderNumber: '0001' });
  });

  it('auto-prints a newly arrived order when ticket printing is enabled', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse({ ticketPrinterEnabled: true })),
      ),
    );

    renderPage();

    // Initial load is empty — seeds the "seen" set without printing.
    await screen.findByTestId('kitchen-empty');
    expect(printTicketMock).not.toHaveBeenCalled();

    // A new order arrives; the next refetch (triggered by the hub) returns it.
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'new-1', orderNumber: '0042' })] }),
      ),
    );
    triggerStatusChange?.();

    await waitFor(() => { expect(printTicketMock).toHaveBeenCalledTimes(1); });
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- first call exists per the toHaveBeenCalledTimes(1) assertion above
    expect(printTicketMock.mock.calls[0]![0]).toMatchObject({ id: 'new-1', orderNumber: '0042' });
  });

  it('does not auto-print the existing backlog on first load', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse({ ticketPrinterEnabled: true })),
      ),
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'a', orderNumber: '0001' })] }),
      ),
    );

    renderPage();

    await screen.findByTestId('kitchen-order-card');
    // Give the auto-print effect a chance to (incorrectly) fire.
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(printTicketMock).not.toHaveBeenCalled();
  });

  it('plays a sound and highlights a newly arrived order when those channels are enabled', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse({ soundAlertEnabled: true, kitchenDisplayEnabled: true })),
      ),
    );

    renderPage();

    // First load is empty — seeds the "seen" set without reacting.
    await screen.findByTestId('kitchen-empty');
    expect(playSoundMock).not.toHaveBeenCalled();

    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'new-1', orderNumber: '0042' })] }),
      ),
    );
    triggerStatusChange?.();

    await waitFor(() => { expect(playSoundMock).toHaveBeenCalledTimes(1); });
    await waitFor(() =>
      expect(screen.getByTestId('kitchen-order-card')).toHaveAttribute('data-highlight', 'true'),
    );
    // The other channels stay silent — only the enabled ones fire (independent toggles).
    expect(printTicketMock).not.toHaveBeenCalled();
    expect(showNotificationMock).not.toHaveBeenCalled();
  });

  it('raises a push notification for a newly arrived order when enabled', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse({ pushNotificationEnabled: true })),
      ),
    );

    renderPage();
    await screen.findByTestId('kitchen-empty');

    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'new-2', orderNumber: '0099' })] }),
      ),
    );
    triggerStatusChange?.();

    await waitFor(() => { expect(showNotificationMock).toHaveBeenCalledTimes(1); });
    // Notification carries a title, a body and the order id as the dedupe tag.
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- first call exists per the toHaveBeenCalledTimes(1) assertion above
    expect(showNotificationMock.mock.calls[0]![2]).toBe('new-2');
  });

  it('does not fire sound, push or highlight for the existing backlog on first load', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(
          shopResponse({
            soundAlertEnabled: true,
            pushNotificationEnabled: true,
            kitchenDisplayEnabled: true,
          }),
        ),
      ),
      http.get(`/api/brands/${brandSlug}/shops/${shopId}/orders/active`, () =>
        HttpResponse.json({ orders: [makeOrder({ id: 'a', orderNumber: '0001' })] }),
      ),
    );

    renderPage();

    const card = await screen.findByTestId('kitchen-order-card');
    await new Promise((resolve) => setTimeout(resolve, 50));
    expect(playSoundMock).not.toHaveBeenCalled();
    expect(showNotificationMock).not.toHaveBeenCalled();
    expect(card).not.toHaveAttribute('data-highlight', 'true');
  });

  it('arms sound and push from the enable-alerts control', async () => {
    server.use(
      http.get(`/api/brands/${brandSlug}/shops/${shopId}`, () =>
        HttpResponse.json(shopResponse({ soundAlertEnabled: true, pushNotificationEnabled: true })),
      ),
    );

    const user = userEvent.setup();
    renderPage();

    const enableButton = await screen.findByTestId('kitchen-enable-alerts');
    await user.click(enableButton);

    expect(primeAudioMock).toHaveBeenCalledTimes(1);
    await waitFor(() => { expect(requestPermissionMock).toHaveBeenCalledTimes(1); });
    // Once armed, the control disappears.
    await waitFor(() =>
      expect(screen.queryByTestId('kitchen-enable-alerts')).not.toBeInTheDocument(),
    );
  });
});
