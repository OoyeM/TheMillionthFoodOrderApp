import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { BrandEdit } from '../BrandEdit';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockBrand = {
  id: 'brand-1',
  slug: 'frietjes',
  name: 'Frietjes',
  contactEmail: 'contact@frietjes.be',
  contactPhone: null,
  isActive: true,
  databaseName: 'frietjes_db',
  staffAuthMethod: 'EmailPassword',
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
        path="/:brandSlug/:lang/admin/brands/:brandId"
        element={<BrandEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/brands/brand-1'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:id', () => HttpResponse.json(mockBrand)),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('BrandEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    expect(nameInput).toHaveValue('Frietjes');

    const emailInput = screen.getByLabelText(/contact email/i);
    expect(emailInput).toHaveValue('contact@frietjes.be');
  });

  it('lets the user edit the name field', async () => {
    const user = userEvent.setup();
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'Frietjes Updated');

    expect(nameInput).toHaveValue('Frietjes Updated');
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:id', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...mockBrand, name: String(capturedBody.name) });
      }),
    );

    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);
    await user.type(nameInput, 'Frietjes Updated');

    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());
    expect(capturedBody).toMatchObject({
      name: 'Frietjes Updated',
      contactEmail: 'contact@frietjes.be',
    });
  });

  it('shows a validation error when name is cleared', async () => {
    const user = userEvent.setup();
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    await user.clear(nameInput);

    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    expect(await screen.findByText(/name is required/i)).toBeInTheDocument();
  });
});
