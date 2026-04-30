import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ProductEdit } from '../ProductEdit';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockProduct = {
  id: 'prod-1',
  productType: 'Simple',
  basePrice: { amount: 4.5, currency: 'EUR' },
  imageUrl: null,
  menuCategoryId: 'cat-1',
  sortOrderInCategory: 1,
  translations: [
    { languageCode: 'nl', name: 'Kleine friet', description: null },
  ],
  allergens: [],
  dietaryTags: [],
  comboItems: null,
  createdAt: '2024-01-01T00:00:00Z',
  updatedAt: '2024-01-01T00:00:00Z',
};

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/products/:productId/edit"
        element={<ProductEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/products/prod-1/edit'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/products/:id', () => HttpResponse.json(mockProduct)),
    http.get('/api/brands/:slug/products/:id/modifier-groups', () => HttpResponse.json([])),
    http.get('/api/brands/:slug/modifier-groups', () => HttpResponse.json([])),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ProductEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    // Base price input — label is "Basisprijs (EUR) *"
    const basePriceInput = await screen.findByLabelText(/basisprijs/i);
    expect(basePriceInput).toHaveValue(4.5);

    // NL name input — label is "Name *" (default active tab is nl)
    const nlNameInput = screen.getByLabelText(/^name/i);
    expect(nlNameInput).toHaveValue('Kleine friet');
  });

  it('lets the user edit the base price', async () => {
    const user = userEvent.setup();
    renderPage();

    const basePriceInput = await screen.findByLabelText(/basisprijs/i);

    await user.clear(basePriceInput);
    await user.type(basePriceInput, '5.99');

    expect(basePriceInput).toHaveValue(5.99);
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/products/:id', async ({ params, request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: params.id,
          productType: 'Simple',
          basePrice: { amount: capturedBody.basePrice, currency: 'EUR' },
          imageUrl: capturedBody.imageUrl ?? null,
          menuCategoryId: 'cat-1',
          sortOrderInCategory: 1,
          translations: capturedBody.translations ?? [],
          allergens: capturedBody.allergens ?? [],
          dietaryTags: capturedBody.dietaryTags ?? [],
          comboItems: null,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-06-02T00:00:00Z',
        });
      }),
    );

    renderPage();

    // Wait for data to load — base price input should reflect fixture value
    const basePriceInput = await screen.findByLabelText(/basisprijs/i);

    // Wait for the Save button to appear (it renders after data loads)
    const saveButton = await screen.findByRole('button', { name: /wijzigingen opslaan/i });

    // Edit the base price
    await user.clear(basePriceInput);
    await user.type(basePriceInput, '5.99');

    // Submit
    await user.click(saveButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());

    expect(capturedBody).toMatchObject({ basePrice: 5.99 });

    // Translations array must contain the NL entry with the original name
    const translations = capturedBody!.translations as Array<{
      languageCode: string;
      name: string;
      description: string | null;
    }>;
    expect(
      translations.some(
        (t) => t.languageCode === 'nl' && t.name === 'Kleine friet' && t.description === null,
      ),
    ).toBe(true);
  });

  it('shows an editable description for the active translation', async () => {
    renderPage();

    // Wait for form data to load first
    await screen.findByLabelText(/basisprijs/i);

    // Description textarea for the active (nl) tab — label is "Beschrijving (optioneel)"
    const descriptionTextarea = screen.getByLabelText(/beschrijving/i);
    expect(descriptionTextarea.tagName.toLowerCase()).toBe('textarea');
  });
});
