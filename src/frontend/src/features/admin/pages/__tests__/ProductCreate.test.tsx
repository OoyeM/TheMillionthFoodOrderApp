import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ProductCreate } from '../ProductCreate';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/products/new" element={<ProductCreate />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/products/new'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ProductCreate', () => {
  it('renders the form with NL tab active and a name input visible', () => {
    renderPage();

    // NL tab button should be present and active (bold / underlined via style)
    const nlTab = screen.getByRole('button', { name: /^nl/i });
    expect(nlTab).toBeInTheDocument();

    // NL name input is rendered when the NL tab is active
    const nlNameInput = screen.getByLabelText(/^name/i);
    expect(nlNameInput).toBeInTheDocument();
    expect(nlNameInput).toHaveValue('');
  });

  it('shows validation error when NL name is empty and Save is clicked', async () => {
    const user = userEvent.setup();
    renderPage();

    // Find and click the submit button without filling in the NL name
    const createButton = screen.getByRole('button', { name: /product aanmaken/i });
    await user.click(createButton);

    // The schema requires NL name; error should appear
    await waitFor(() => {
      expect(screen.getByText(/dutch name is required/i)).toBeInTheDocument();
    });
  });

  it('submits with correct payload — base price + NL translation', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/brands/:slug/products', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'prod-new',
            productType: 'Simple',
            basePrice: { amount: capturedBody.basePrice, currency: 'EUR' },
            imageUrl: null,
            menuCategoryId: null,
            sortOrderInCategory: 0,
            translations: capturedBody.translations,
            allergens: [],
            dietaryTags: [],
            comboItems: null,
            createdAt: '2024-06-01T00:00:00Z',
            updatedAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Fill in the base price
    const basePriceInput = screen.getByLabelText(/basisprijs/i);
    await user.clear(basePriceInput);
    await user.type(basePriceInput, '3.5');

    // Fill in the NL name
    const nlNameInput = screen.getByLabelText(/^name/i);
    await user.type(nlNameInput, 'Kleine friet');

    // Submit the form
    const createButton = screen.getByRole('button', { name: /product aanmaken/i });
    await user.click(createButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    // Assert basePrice
    expect(capturedBody!.basePrice).toBe(3.5);

    // Assert NL translation is present
    const translations = capturedBody!.translations as {
      languageCode: string;
      name: string;
    }[];
    expect(
      translations.some((t) => t.languageCode === 'nl' && t.name === 'Kleine friet'),
    ).toBe(true);
  });

  it('Cancel button is present and clickable', async () => {
    const user = userEvent.setup();
    renderPage();

    const cancelButton = screen.getByRole('button', { name: /annuleren/i });
    expect(cancelButton).toBeInTheDocument();

    // Clicking cancel should not throw
    await user.click(cancelButton);
  });
});
