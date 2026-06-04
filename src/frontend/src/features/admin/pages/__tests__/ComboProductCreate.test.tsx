import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ComboProductCreate } from '../ComboProductCreate';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockSimpleProduct = {
  id: 'prod-simple-1',
  productType: 'Simple',
  name: 'Kleine friet',
  basePrice: { amount: 3.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 1,
  allergens: [],
  dietaryTags: [],
  createdAt: '2024-01-01T00:00:00Z',
};

const mockSimpleProduct2 = {
  id: 'prod-simple-2',
  productType: 'Simple',
  name: 'Cola',
  basePrice: { amount: 2.0, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 2,
  allergens: [],
  dietaryTags: [],
  createdAt: '2024-01-01T00:00:00Z',
};

const mockProductsResponse = [mockSimpleProduct, mockSimpleProduct2];

const mockCreatedCombo = {
  id: 'prod-combo-new',
  productType: 'Combo',
  basePrice: { amount: 5.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: null,
  sortOrderInCategory: 0,
  translations: [{ languageCode: 'nl', name: 'Friet + Drank', description: null }],
  allergens: [],
  dietaryTags: [],
  comboItems: [
    { componentProductId: 'prod-simple-1', name: 'Kleine friet', sortOrder: 0 },
    { componentProductId: 'prod-simple-2', name: 'Cola', sortOrder: 1 },
  ],
  createdAt: '2024-06-01T00:00:00Z',
  updatedAt: '2024-06-01T00:00:00Z',
};

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/combo-products/new"
        element={<ComboProductCreate />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/combo-products/new'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/products', () => HttpResponse.json(mockProductsResponse)),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ComboProductCreate', () => {
  it('renders the create form with empty fields', async () => {
    renderPage();

    // Base price input — label text is "Bundelprijs (EUR)"
    const basePriceInput = await screen.findByLabelText(/bundelprijs/i);
    expect(basePriceInput).toHaveValue(0);

    // NL name input — label is "Name *" (default active tab is nl)
    const nlNameInput = screen.getByLabelText(/^name/i);
    expect(nlNameInput).toHaveValue('');
  });

  it('user can type a combo name', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for form to render
    const nlNameInput = await screen.findByLabelText(/^name/i);

    await user.type(nlNameInput, 'Friet + Drank');

    expect(nlNameInput).toHaveValue('Friet + Drank');
  });

  it('submits create payload with entered name and a selected component', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/brands/:slug/combo-products', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(mockCreatedCombo, { status: 201 });
      }),
    );

    renderPage();

    // Wait for the simple products to appear in the picker
    await screen.findByText('Kleine friet');

    // Type a combo name
    const nlNameInput = screen.getByLabelText(/^name/i);
    await user.type(nlNameInput, 'Friet + Drank');

    // Type a base price
    const basePriceInput = screen.getByLabelText(/bundelprijs/i);
    await user.clear(basePriceInput);
    await user.type(basePriceInput, '5.5');

    // Select both component products (schema requires min 2)
    await user.click(screen.getByText('Kleine friet'));
    await user.click(screen.getByText('Cola'));

    // Find and click the create/submit button — label from i18n: "Combo aanmaken"
    const createButton = screen.getByRole('button', { name: /combo aanmaken/i });
    await user.click(createButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    // Assert the POST body contains the name and the component product id
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- capturedBody is non-null per the waitFor assertion above
    const translations = capturedBody!.translations as {
      languageCode: string;
      name: string;
    }[];
    expect(translations.some((t) => t.languageCode === 'nl' && t.name === 'Friet + Drank')).toBe(
      true,
    );
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- capturedBody is non-null per the waitFor assertion above
    const componentIds = capturedBody!.componentProductIds as string[];
    expect(componentIds).toContain('prod-simple-1');
    expect(componentIds).toContain('prod-simple-2');
  });

  it('shows available simple products in the picker section', async () => {
    renderPage();

    // The simple product from the mock should appear in the picker
    const productName = await screen.findByText('Kleine friet');
    expect(productName).toBeInTheDocument();
  });
});
