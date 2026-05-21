import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor, fireEvent } from '@testing-library/react';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import '../../../../i18n/config';

// Mock SignalR so it never tries to open a WebSocket
vi.mock('../../../../api/useOrderUpdates', () => ({
  useOrderUpdates: () => ({ status: 'connected' as const }),
}));

import { NewOrderPage } from '../NewOrderPage';

const brandSlug = 'frietjes';
const shopId = '00000000-0000-0000-0000-000000000001';
const categoryId = 'cat-1';

const mockCategories = [
  { id: categoryId, name: 'Frieten', sortOrder: 0 },
];

const mockProducts = [
  {
    id: 'prod-1',
    name: 'Frietje Klein',
    basePrice: { amount: 3.5, currency: 'EUR' },
    imageUrl: null,
    sortOrderInCategory: 0,
    allergens: [],
    dietaryTags: [],
  },
  {
    id: 'prod-2',
    name: 'Frietje Groot',
    basePrice: { amount: 5.0, currency: 'EUR' },
    imageUrl: null,
    sortOrderInCategory: 1,
    allergens: [],
    dietaryTags: [],
  },
];

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/pos/shops/:shopId/order" element={<NewOrderPage />} />
    </Routes>,
    { initialEntries: [`/${brandSlug}/nl/pos/shops/${shopId}/order`] },
  );
}

describe('NewOrderPage', () => {
  beforeEach(() => {
    server.use(
      http.get(`/api/brands/${brandSlug}/menu-categories`, () =>
        HttpResponse.json(mockCategories),
      ),
      http.get(`/api/brands/${brandSlug}/menu-categories/${categoryId}/products`, () =>
        HttpResponse.json(mockProducts),
      ),
      http.get(`/api/brands/${brandSlug}/products/:productId/modifier-groups`, () =>
        HttpResponse.json([]),
      ),
    );
  });

  it('renders the page title', async () => {
    renderPage();
    expect(await screen.findByText(/nieuwe bestelling/i)).toBeInTheDocument();
  });

  it('renders category tabs after loading', async () => {
    renderPage();
    expect(await screen.findByRole('button', { name: 'Frieten' })).toBeInTheDocument();
  });

  it('renders product tiles after loading', async () => {
    renderPage();
    expect(await screen.findByTestId('product-tile-prod-1')).toBeInTheDocument();
    expect(screen.getByTestId('product-tile-prod-2')).toBeInTheDocument();
  });

  it('shows the table number input only when EatIn is selected', async () => {
    renderPage();

    // Wait for page to load
    await screen.findByTestId('pos-ticket');

    // Initially Pickup — no table number input
    expect(screen.queryByTestId('pos-table-number-input')).not.toBeInTheDocument();

    // Switch to EatIn
    fireEvent.click(screen.getByRole('button', { name: /ter plaatse/i }));

    await waitFor(() => {
      expect(screen.getByTestId('pos-table-number-input')).toBeInTheDocument();
    });
  });

  it('disables Place order button when EatIn is selected without a table number', async () => {
    renderPage();
    await screen.findByTestId('pos-ticket');

    // Add a product — wait for tile, click it, wait for non-disabled Add button, click
    const tile = await screen.findByTestId('product-tile-prod-1');
    fireEvent.click(tile);

    // Wait for the "Add to order" button to appear and be enabled (modifier query resolved)
    const addBtn = await screen.findByRole('button', { name: /toevoegen aan bon/i });
    await waitFor(() => expect(addBtn).not.toBeDisabled());
    fireEvent.click(addBtn);

    // Wait for item to appear in ticket (subtotal becomes non-zero once item is added)
    await waitFor(() =>
      expect(screen.getByTestId('pos-subtotal')).toHaveTextContent(/3,50/),
    );

    // Switch to EatIn
    fireEvent.click(screen.getByRole('button', { name: /ter plaatse/i }));

    await waitFor(() => {
      expect(screen.getByTestId('pos-place-order-btn')).toBeDisabled();
    });
  });

  it('enables Place order button when EatIn and table number are set', async () => {
    renderPage();
    await screen.findByTestId('pos-ticket');

    // Add a product
    const tile = await screen.findByTestId('product-tile-prod-1');
    fireEvent.click(tile);

    const addBtn = await screen.findByRole('button', { name: /toevoegen aan bon/i });
    await waitFor(() => expect(addBtn).not.toBeDisabled());
    fireEvent.click(addBtn);

    // Wait for item to appear in ticket (subtotal becomes non-zero once item is added)
    await waitFor(() =>
      expect(screen.getByTestId('pos-subtotal')).toHaveTextContent(/3,50/),
    );

    // Switch to EatIn and enter table number
    fireEvent.click(screen.getByRole('button', { name: /ter plaatse/i }));
    const tableInput = await screen.findByTestId('pos-table-number-input');
    fireEvent.change(tableInput, { target: { value: 'T-5' } });

    await waitFor(() => {
      expect(screen.getByTestId('pos-place-order-btn')).not.toBeDisabled();
    });
  });

  it('submits with tableNumber when EatIn order is placed', async () => {
    let capturedBody: unknown = null;

    server.use(
      http.post(`/api/brands/${brandSlug}/shops/${shopId}/orders`, async ({ request }) => {
        capturedBody = await request.json();
        return HttpResponse.json(
          {
            id: 'order-123',
            orderNumber: 'ABC123',
            shopId,
            brandSlug,
            orderType: 'EatIn',
            paymentMethod: 'CashAtPickup',
            statusName: 'Placed',
            customerName: null,
            tableNumber: 'T-12',
            items: [],
            vatRatePercent: 21,
            subtotalGross: 3.5,
            totalVatAmount: 0.61,
            totalNet: 2.89,
            totalGross: 3.5,
            createdAt: new Date().toISOString(),
          },
          { status: 201 },
        );
      }),
    );

    renderPage();
    await screen.findByTestId('pos-ticket');

    // Add a product
    const tile = await screen.findByTestId('product-tile-prod-1');
    fireEvent.click(tile);

    const addBtn = await screen.findByRole('button', { name: /toevoegen aan bon/i });
    await waitFor(() => expect(addBtn).not.toBeDisabled());
    fireEvent.click(addBtn);

    // Wait for item to appear in ticket (subtotal becomes non-zero once item is added)
    await waitFor(() =>
      expect(screen.getByTestId('pos-subtotal')).toHaveTextContent(/3,50/),
    );

    // Switch to EatIn and enter table number
    fireEvent.click(screen.getByRole('button', { name: /ter plaatse/i }));
    const tableInput = await screen.findByTestId('pos-table-number-input');
    fireEvent.change(tableInput, { target: { value: 'T-12' } });

    // Submit
    await waitFor(() =>
      expect(screen.getByTestId('pos-place-order-btn')).not.toBeDisabled(),
    );
    fireEvent.click(screen.getByTestId('pos-place-order-btn'));

    await waitFor(() => {
      expect(capturedBody).toMatchObject({
        orderType: 'EatIn',
        tableNumber: 'T-12',
        paymentMethod: 'CashAtPickup',
      });
    });
  });
});
