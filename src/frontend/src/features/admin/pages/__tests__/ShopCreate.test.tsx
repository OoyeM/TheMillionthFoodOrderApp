import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ShopCreate } from '../ShopCreate';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/shops/new" element={<ShopCreate />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/shops/new'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopCreate', () => {
  it('renders the form with name, slug, and email inputs', async () => {
    renderPage();

    // The form heading should be present
    expect(await screen.findByRole('heading', { name: /create shop/i })).toBeInTheDocument();

    // Name input (id="name")
    const nameInput = screen.getByLabelText(/^name/i);
    expect(nameInput).toBeInTheDocument();
    expect(nameInput).toHaveValue('');

    // Slug input (id="slug")
    const slugInput = screen.getByLabelText(/^slug/i);
    expect(slugInput).toBeInTheDocument();
    expect(slugInput).toHaveValue('');

    // Contact email input (id="contactEmail")
    const emailInput = screen.getByLabelText(/contact email/i);
    expect(emailInput).toBeInTheDocument();
    expect(emailInput).toHaveValue('');
  });

  it('typing in the name field auto-derives the slug value', async () => {
    const user = userEvent.setup();
    renderPage();

    const nameInput = await screen.findByLabelText(/^name/i);
    const slugInput = screen.getByLabelText(/^slug/i);

    await user.type(nameInput, 'Gent Centrum');

    // Slug should be the lowercase-hyphenated version of the name
    expect(slugInput).toHaveValue('gent-centrum');
  });

  it('submits with correct payload (name, slug, address, contactEmail)', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/brands/:slug/shops', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'shop-new',
            name: capturedBody.name,
            slug: capturedBody.slug,
            address: capturedBody.address,
            contactEmail: capturedBody.contactEmail,
            contactPhone: null,
            isActive: true,
            createdAt: '2024-06-01T00:00:00Z',
            updatedAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Fill in the name (slug auto-derives)
    const nameInput = await screen.findByLabelText(/^name/i);
    await user.type(nameInput, 'Gent Centrum');

    // Fill address fields
    await user.type(screen.getByLabelText(/street/i), 'Veldstraat');
    await user.type(screen.getByLabelText(/number/i), '12');
    await user.type(screen.getByLabelText(/postal code/i), '9000');
    await user.type(screen.getByLabelText(/city/i), 'Gent');

    // Fill contact email
    await user.type(screen.getByLabelText(/contact email/i), 'gent@frietjes.be');

    // Submit
    await user.click(screen.getByRole('button', { name: /create shop/i }));

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    expect(capturedBody!.name).toBe('Gent Centrum');
    expect(capturedBody!.slug).toBe('gent-centrum');
    expect(capturedBody!.contactEmail).toBe('gent@frietjes.be');

    const address = capturedBody!.address as Record<string, string>;
    expect(address.street).toBe('Veldstraat');
    expect(address.number).toBe('12');
    expect(address.city).toBe('Gent');
    expect(address.postalCode).toBe('9000');
    expect(address.country).toBe('BE');
  });

  it('shows validation error when required fields are empty', async () => {
    const user = userEvent.setup();
    renderPage();

    // Submit without filling anything
    const submitButton = await screen.findByRole('button', { name: /create shop/i });
    await user.click(submitButton);

    // Zod validation messages should appear
    await waitFor(() => {
      expect(screen.getByText(/name is required/i)).toBeInTheDocument();
    });
  });
});
