import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { MenuCategoryList } from '../MenuCategoryList';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/menu-categories"
        element={<MenuCategoryList />}
      />
      {/* Catch-all so navigation tests can assert the destination */}
      <Route path="/:brandSlug/:lang/admin/menu-categories/:id" element={<div>Edit Page</div>} />
      <Route path="/:brandSlug/:lang/admin/menu-categories/new" element={<div>Create Page</div>} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/menu-categories'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/menu-categories', () =>
      HttpResponse.json([
        {
          id: 'cat-1',
          name: 'Frietjes',
          sortOrder: 1,
          imageUrl: null,
          productCount: 3,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('MenuCategoryList', () => {
  it('renders category name "Frietjes" after data loads', async () => {
    renderPage();

    const categoryName = await screen.findByText('Frietjes');
    expect(categoryName).toBeInTheDocument();
  });

  it('Delete button calls DELETE /api/brands/:slug/menu-categories/:id', async () => {
    const user = userEvent.setup();

    let deleteWasCalled = false;
    server.use(
      http.delete('/api/brands/:slug/menu-categories/:id', ({ params }) => {
        expect(params.id).toBe('cat-1');
        deleteWasCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    // Confirm dialog must return true for the deletion to proceed
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderPage();

    // Wait for list to render
    await screen.findByText('Frietjes');

    const deleteButton = screen.getByRole('button', { name: /delete/i });
    await user.click(deleteButton);

    await waitFor(() => { expect(deleteWasCalled).toBe(true); });

    confirmSpy.mockRestore();
  });

  it('renders a "Create Category" button', async () => {
    renderPage();

    // Wait for page to settle (list loaded)
    await screen.findByText('Frietjes');

    const createButton = screen.getByRole('button', { name: /create category/i });
    expect(createButton).toBeInTheDocument();
  });

  it('clicking a row navigates to the edit route', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the row to appear
    await screen.findByText('Frietjes');

    // The row itself (the <tr>) is clickable; clicking the category name cell navigates
    const categoryName = screen.getByText('Frietjes');
    await user.click(categoryName);

    // After navigation, the edit page stub should be rendered
    await screen.findByText('Edit Page');
  });
});
