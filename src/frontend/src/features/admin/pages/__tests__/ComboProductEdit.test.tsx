import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ComboProductEdit } from '../ComboProductEdit';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockCombo = {
  id: 'prod-combo-1',
  productType: 'Combo',
  basePrice: { amount: 9.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: null,
  sortOrderInCategory: 0,
  translations: [
    { languageCode: 'nl', name: 'Combo Friet+Drink', description: null },
    { languageCode: 'fr', name: 'Combo Frites+Boisson', description: null },
  ],
  allergens: [],
  dietaryTags: [],
  comboItems: [
    { componentProductId: 'prod-1', name: 'Kleine friet', sortOrder: 0 },
    { componentProductId: 'prod-2', name: 'Cola', sortOrder: 1 },
  ],
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-06-01T00:00:00Z',
};

const mockProducts = [
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
  {
    id: 'prod-2',
    productType: 'Simple',
    name: 'Cola',
    basePrice: { amount: 2.0, currency: 'EUR' },
    imageUrl: null,
    menuCategoryId: 'cat-1',
    sortOrderInCategory: 2,
    allergens: [],
    dietaryTags: [],
    createdAt: '2024-01-01T00:00:00Z',
  },
];

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/products/combo/:productId/edit"
        element={<ComboProductEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/products/combo/prod-combo-1/edit'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/products/:id', () => HttpResponse.json(mockCombo)),
    http.get('/api/brands/:slug/products', () => HttpResponse.json(mockProducts)),
    http.get('/api/brands/:slug/products/:id/modifier-groups', () => HttpResponse.json([])),
    http.get('/api/brands/:slug/modifier-groups', () => HttpResponse.json([])),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ComboProductEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    // Base price input — label is "Bundelprijs (EUR) *"
    const basePriceInput = await screen.findByLabelText(/bundelprijs/i);
    expect(basePriceInput).toHaveValue(9.5);

    // NL name input — label is "Name *" (default active tab is nl)
    const nlNameInput = screen.getByLabelText(/^name/i);
    expect(nlNameInput).toHaveValue('Combo Friet+Drink');
  });

  it('lets the user edit the base price', async () => {
    const user = userEvent.setup();
    renderPage();

    const basePriceInput = await screen.findByLabelText(/bundelprijs/i);

    await user.clear(basePriceInput);
    await user.type(basePriceInput, '12.99');

    expect(basePriceInput).toHaveValue(12.99);
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/combo-products/:id', async ({ request, params }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: params.id,
          productType: 'Combo',
          basePrice: { amount: capturedBody.basePrice, currency: 'EUR' },
          imageUrl: capturedBody.imageUrl ?? null,
          menuCategoryId: null,
          sortOrderInCategory: 0,
          translations: capturedBody.translations ?? [],
          allergens: [],
          dietaryTags: [],
          comboItems: (capturedBody.componentProductIds as string[]).map((id, i) => ({
            componentProductId: id,
            name: `Product ${String(i + 1)}`,
            sortOrder: i,
          })),
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-06-02T00:00:00Z',
        });
      }),
    );

    renderPage();

    // Wait for data to load — the save button text is the Dutch translation
    const basePriceInput = await screen.findByLabelText(/bundelprijs/i);

    // Wait for the Save button to appear (it renders after data loads)
    const saveButton = await screen.findByRole('button', { name: /wijzigingen opslaan/i });

    // Edit the base price
    await user.clear(basePriceInput);
    await user.type(basePriceInput, '12.99');

    // Submit
    await user.click(saveButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    expect(capturedBody).toMatchObject({
      basePrice: 12.99,
      componentProductIds: ['prod-1', 'prod-2'],
    });

    // Translations array must contain the NL entry with the original name
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- capturedBody is non-null per the waitFor assertion above
    const translations = capturedBody!.translations as {
      languageCode: string;
      name: string;
    }[];
    expect(translations.some((t) => t.languageCode === 'nl' && t.name === 'Combo Friet+Drink')).toBe(
      true,
    );
  });

  it('shows component products selected from fetched data', async () => {
    renderPage();

    // Both component product names must appear in the selected list
    await screen.findByText('Kleine friet');
    expect(screen.getByText('Cola')).toBeInTheDocument();
  });
});
