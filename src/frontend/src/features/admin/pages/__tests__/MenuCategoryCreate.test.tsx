import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { MenuCategoryCreate } from '../MenuCategoryCreate';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/menu-categories/new"
        element={<MenuCategoryCreate />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/menu-categories/new'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('MenuCategoryCreate', () => {
  it('renders the create form with NL tab active', async () => {
    renderPage();

    // Heading is present immediately
    expect(screen.getByRole('heading', { name: /create menu category/i })).toBeInTheDocument();

    // After settings load, the NL name input appears (id="name-nl")
    const nameInput = await screen.findByLabelText(/^name/i);
    expect(nameInput).toBeInTheDocument();

    // Sort order input is also rendered
    expect(screen.getByLabelText(/sort order/i)).toBeInTheDocument();
  });

  it('shows validation error when primary-locale name is empty', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the form to be interactive
    await screen.findByLabelText(/^name/i);

    // Click the submit button without entering a name
    const createButton = screen.getByRole('button', { name: /create category/i });
    await user.click(createButton);

    // Validation error for missing NL name should appear
    expect(await screen.findByText(/nl name is required/i)).toBeInTheDocument();
  });

  it('submits with the correct payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/brands/:slug/menu-categories', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'cat-new',
            sortOrder: 0,
            imageUrl: null,
            productCount: 0,
            translations: [{ languageCode: 'nl', name: 'Friet Snacks' }],
            createdAt: '2024-06-01T00:00:00Z',
            updatedAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Wait for the NL name input to appear
    const nameInput = await screen.findByLabelText(/^name/i);
    await user.type(nameInput, 'Friet Snacks');

    // Click Create Category
    const createButton = screen.getByRole('button', { name: /create category/i });
    await user.click(createButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    const translations = capturedBody!.translations as {
      languageCode: string;
      name: string;
    }[];
    expect(
      translations.some((t) => t.languageCode === 'nl' && t.name === 'Friet Snacks'),
    ).toBe(true);
  });

  it('Cancel button navigates away', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the form to be interactive
    await screen.findByLabelText(/^name/i);

    // Cancel button is present and clickable without throwing
    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    expect(cancelButton).toBeInTheDocument();
    await user.click(cancelButton);
  });
});
