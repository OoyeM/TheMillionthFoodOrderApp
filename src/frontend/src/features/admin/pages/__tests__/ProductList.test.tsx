import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ProductList } from '../ProductList';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/products" element={<ProductList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/products'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/products', () =>
      HttpResponse.json([
        {
          id: 'prod-1',
          productType: 'Simple',
          name: 'Kleine friet',
          basePrice: { amount: 3.5, currency: 'EUR' },
          imageUrl: null,
          menuCategoryId: 'cat-1',
          sortOrderInCategory: 1,
          allergens: [],
          dietaryTags: [],
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ProductList', () => {
  it('renders product name "Kleine friet" after data loads', async () => {
    renderPage();

    const productName = await screen.findByText('Kleine friet');
    expect(productName).toBeInTheDocument();
  });

  it('Create Product button is present', async () => {
    renderPage();

    // Wait for list to render so we're past the loading state
    await screen.findByText('Kleine friet');

    const createButton = screen.getByRole('button', { name: /\+ create product/i });
    expect(createButton).toBeInTheDocument();
  });

  it('clicking a row navigates to the product edit route', async () => {
    const user = userEvent.setup();

    renderPage();

    // Wait for the product row to appear
    const productName = await screen.findByText('Kleine friet');

    // Click the table row that contains the product name
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the product name cell is rendered inside a table row, so closest('tr') always resolves
    const row = productName.closest('tr')!;
    await user.click(row);

    // After navigation the URL should change to the edit route.
    // We verify by checking that the product name cell is no longer visible
    // (the route has changed away from ProductList), OR we can assert on
    // location. Since MemoryRouter doesn't expose location easily, we rely
    // on the fact that ProductList is only mounted for its own route and the
    // new route renders nothing — so the table disappears.
    await waitFor(() => {
      expect(screen.queryByText('Kleine friet')).not.toBeInTheDocument();
    });
  });

  it('delete button calls DELETE API after window.confirm', async () => {
    const user = userEvent.setup();

    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    let deleteCalled = false;
    server.use(
      http.delete('/api/brands/:slug/products/:id', () => {
        deleteCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    renderPage();

    // Wait for product row to appear
    await screen.findByText('Kleine friet');

    const deleteButton = screen.getByRole('button', { name: /delete/i });
    await user.click(deleteButton);

    // window.confirm should have been called with the product name
    expect(confirmSpy).toHaveBeenCalled();
    const confirmMessage = confirmSpy.mock.calls[0]?.[0] ?? '';
    expect(confirmMessage).toMatch(/kleine friet/i);

    // DELETE endpoint should have been hit
    await waitFor(() => { expect(deleteCalled).toBe(true); });

    confirmSpy.mockRestore();
  });
});
