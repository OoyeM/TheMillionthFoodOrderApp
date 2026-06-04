/**
 * t-13 — Submitting posts the correct in-store payload to the in-store route
 *         and clears the order on success [AC5]
 */
import { describe, it, expect } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { http, HttpResponse } from 'msw';
import { server } from '@/test/msw/server';
import { renderWithProviders } from '@/test/testUtils';
import { PosOrderProvider, useOrderState } from '../context/PosOrderContext';
import { useCreateInStoreOrder } from '../hooks/useCreateInStoreOrder';

// ── Test component that exercises the full mutation ──────────────────────────

interface TestOrderFormProps {
  brandSlug: string;
  shopId: string;
}

function TestOrderForm({ brandSlug, shopId }: TestOrderFormProps) {
  const { state, addItem, setOrderType, setTableNumber } = useOrderState();
  const mutation = useCreateInStoreOrder(brandSlug, shopId);

  return (
    <div>
      <button
        type="button"
        data-testid="add-item"
        onClick={() =>
          { addItem({
            productId: 'prod-1',
            productName: 'Kleine friet',
            quantity: 2,
            unitGrossPrice: 3.5,
            selectedModifiers: [
              { modifierId: 'mod-1', modifierName: 'Mayonaise', priceAdjustment: 0 },
            ],
          }); }
        }
      >
        Add item
      </button>

      <button
        type="button"
        data-testid="set-eatin"
        onClick={() => {
          setOrderType('EatIn');
          setTableNumber(7);
        }}
      >
        Set EatIn table 7
      </button>

      <button
        type="button"
        data-testid="submit"
        onClick={() => { void mutation.mutateAsync({}); }}
      >
        Submit
      </button>

      {mutation.isSuccess && (
        <p data-testid="success">Order placed: {mutation.data.orderNumber}</p>
      )}
      {mutation.isError && <p data-testid="error">Error</p>}

      <p data-testid="item-count">{state.items.length} items</p>
    </div>
  );
}

function renderTestForm(brandSlug = 'frietjes', shopId = 'shop-1') {
  return renderWithProviders(
    <PosOrderProvider>
      <TestOrderForm brandSlug={brandSlug} shopId={shopId} />
    </PosOrderProvider>,
    { initialEntries: ['/frietjes/nl/pos'] },
  );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('useCreateInStoreOrder (t-13)', () => {
  it('posts to the in-store route with orderType, items, and paymentMethod', async () => {
    let capturedBody: Record<string, unknown> | null = null;
    let capturedUrl = '';

    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedUrl = `/api/brands/${String(params.slug)}/shops/${String(params.shopId)}/orders/in-store`;
        capturedBody = await request.json() as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'order-new',
            orderNumber: 'ORD-001',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: capturedBody.orderType,
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 6,
            subtotalGross: 7,
            totalVatAmount: 0.4,
            totalNet: 6.6,
            totalGross: 7,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
          },
          { status: 201 },
        );
      }),
    );

    renderTestForm();

    // Add an item
    fireEvent.click(screen.getByTestId('add-item'));

    // Submit
    fireEvent.click(screen.getByTestId('submit'));

    await waitFor(() => {
      expect(screen.getByTestId('success')).toBeInTheDocument();
    });

    // Verify payload shape
    expect(capturedUrl).toContain('/api/brands/frietjes/shops/shop-1/orders/in-store');
    expect(capturedBody).not.toBeNull();
    /* eslint-disable @typescript-eslint/no-non-null-assertion -- capturedBody asserted non-null on the line above */
    expect(capturedBody!.orderType).toBe('Pickup');
    expect(capturedBody!.paymentMethod).toBe('CashAtPickup');
    expect(Array.isArray(capturedBody!.items)).toBe(true);

    const items = capturedBody!.items as {
      productId: string;
      quantity: number;
      selectedModifierIds: string[];
    }[];
    /* eslint-enable @typescript-eslint/no-non-null-assertion */
    expect(items).toHaveLength(1);
    expect(items[0]?.productId).toBe('prod-1');
    expect(items[0]?.quantity).toBe(2);
    expect(items[0]?.selectedModifierIds).toEqual(['mod-1']);
  });

  it('includes tableNumber in the payload for EatIn orders', async () => {
    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedBody = await request.json() as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'order-eatin',
            orderNumber: 'ORD-002',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: 'EatIn',
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 21,
            subtotalGross: 7,
            totalVatAmount: 1.21,
            totalNet: 5.79,
            totalGross: 7,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
            tableNumber: 7,
            createdByStaffId: 'staff-1',
          },
          { status: 201 },
        );
      }),
    );

    renderTestForm();

    // Add item and set EatIn with table 7
    fireEvent.click(screen.getByTestId('add-item'));
    fireEvent.click(screen.getByTestId('set-eatin'));

    // Submit
    fireEvent.click(screen.getByTestId('submit'));

    await waitFor(() => {
      expect(screen.getByTestId('success')).toBeInTheDocument();
    });

    expect(capturedBody).not.toBeNull();
    /* eslint-disable @typescript-eslint/no-non-null-assertion -- capturedBody asserted non-null on the line above */
    expect(capturedBody!.orderType).toBe('EatIn');
    expect(capturedBody!.tableNumber).toBe(7);
    /* eslint-enable @typescript-eslint/no-non-null-assertion */
  });

  it('clears the order items after successful submission', async () => {
    // Default handler returns 201
    renderTestForm();

    fireEvent.click(screen.getByTestId('add-item'));

    // Verify item was added
    await waitFor(() => {
      expect(screen.getByTestId('item-count').textContent).toBe('1 items');
    });

    // Submit
    fireEvent.click(screen.getByTestId('submit'));

    // After success, order should be cleared
    await waitFor(() => {
      expect(screen.getByTestId('item-count').textContent).toBe('0 items');
    });
  });

  it('does NOT include tableNumber in payload for Pickup orders', async () => {
    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request, params }) => {
        capturedBody = await request.json() as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'order-pickup',
            orderNumber: 'ORD-003',
            shopId: params.shopId,
            brandSlug: params.slug,
            orderType: 'Pickup',
            statusName: 'New',
            customerName: null,
            items: [],
            vatRatePercent: 6,
            subtotalGross: 7,
            totalVatAmount: 0.4,
            totalNet: 6.6,
            totalGross: 7,
            createdAt: '2024-06-01T10:00:00Z',
            paymentMethod: 'CashAtPickup',
          },
          { status: 201 },
        );
      }),
    );

    renderTestForm();

    fireEvent.click(screen.getByTestId('add-item'));
    fireEvent.click(screen.getByTestId('submit'));

    await waitFor(() => {
      expect(screen.getByTestId('success')).toBeInTheDocument();
    });

    expect(capturedBody).not.toBeNull();
    // tableNumber must be absent for Pickup orders
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- capturedBody asserted non-null on the line above
    expect('tableNumber' in capturedBody!).toBe(false);
    // Only the expected top-level keys should be present (no accidental extras)
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- capturedBody asserted non-null above
    const topLevelKeys = Object.keys(capturedBody!).sort();
    expect(topLevelKeys).toEqual(['items', 'orderType', 'paymentMethod'].sort());
  });
});
