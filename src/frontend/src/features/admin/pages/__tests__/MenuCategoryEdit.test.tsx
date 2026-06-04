import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { MenuCategoryEdit } from '../MenuCategoryEdit';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockCategory = {
  id: 'cat-1',
  sortOrder: 5,
  imageUrl: null,
  productCount: 3,
  translations: [
    { languageCode: 'nl', name: 'Frietjes' },
    { languageCode: 'fr', name: 'Frites' },
  ],
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
        path="/:brandSlug/:lang/admin/menu-categories/:categoryId/edit"
        element={<MenuCategoryEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/menu-categories/cat-1/edit'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/menu-categories/:id', () => HttpResponse.json(mockCategory)),
    http.get('/api/brands/:slug/menu-categories/:id/products', () => HttpResponse.json([])),
    http.get('/api/brands/:slug/settings', () =>
      HttpResponse.json({
        id: 'settings-1',
        defaultLanguage: 'nl',
        timezone: 'Europe/Brussels',
        currency: 'EUR',
        logoUrl: null,
        customDomain: null,
        colors: null,
        typography: null,
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-01T00:00:00Z',
      }),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('MenuCategoryEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    // Wait for data to load — sortOrder input is a number input
    const sortOrderInput = await screen.findByLabelText(/sort order/i);
    expect(sortOrderInput).toHaveValue(5);

    // NL translation name — default active tab is 'nl'
    const nlNameInput = screen.getByLabelText(/^name/i);
    expect(nlNameInput).toHaveValue('Frietjes');
  });

  it('lets the user edit the sort order', async () => {
    const user = userEvent.setup();
    renderPage();

    const sortOrderInput = await screen.findByLabelText(/sort order/i);

    await user.clear(sortOrderInput);
    await user.type(sortOrderInput, '7');

    expect(sortOrderInput).toHaveValue(7);
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/menu-categories/:id', async ({ params, request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: params.id,
          sortOrder: capturedBody.sortOrder,
          imageUrl: capturedBody.imageUrl ?? null,
          productCount: 3,
          translations: capturedBody.translations ?? [],
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-06-02T00:00:00Z',
        });
      }),
    );

    renderPage();

    // Wait for data to load
    const sortOrderInput = await screen.findByLabelText(/sort order/i);

    // Edit the sort order
    await user.clear(sortOrderInput);
    await user.type(sortOrderInput, '7');

    // Submit
    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    expect(capturedBody).toMatchObject({ sortOrder: 7 });

    // Translations array must contain the NL entry with the original name
    const translations = capturedBody!.translations as {
      languageCode: string;
      name: string;
    }[];
    expect(
      translations.some((t) => t.languageCode === 'nl' && t.name === 'Frietjes'),
    ).toBe(true);
  });

  it('shows the heading', async () => {
    renderPage();

    // The heading renders after data loads (the page shows a loading state before)
    expect(
      await screen.findByRole('heading', { level: 1, name: /edit menu category/i }),
    ).toBeInTheDocument();
  });
});
