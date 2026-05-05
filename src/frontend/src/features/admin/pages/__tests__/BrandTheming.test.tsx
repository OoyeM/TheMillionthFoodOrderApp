import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { BrandTheming } from '../BrandTheming';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockSettings = {
  id: 'settings-1',
  defaultLanguage: 'nl',
  timezone: 'Europe/Brussels',
  currency: 'EUR',
  logoUrl: null,
  customDomain: null,
  colors: {
    primary: '#111827',
    secondary: '#6b7280',
    accent: '#2563eb',
  },
  typography: {
    headingFontFamily: 'System Default',
    bodyFontFamily: 'System Default',
  },
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
        path="/:brandSlug/:lang/admin/theming"
        element={<BrandTheming />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/theming'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/settings', () => HttpResponse.json(mockSettings)),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('BrandTheming', () => {
  it('renders the theming form with fetched colors populated', async () => {
    renderPage();

    // Primary color — label is "Primary"
    const primaryInput = await screen.findByLabelText(/^primary$/i);
    expect(primaryInput).toBeInTheDocument();
  });

  it('renders heading font selector with fetched value', async () => {
    renderPage();

    const headingFontSelect = await screen.findByLabelText(/heading font/i);
    expect(headingFontSelect).toHaveValue('System Default');
  });

  it('submits the update with edited colors', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/settings/theming', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({ ...mockSettings });
      }),
    );

    renderPage();

    // Wait for data to load
    await screen.findByLabelText(/heading font/i);

    const saveButton = screen.getByRole('button', { name: /save theming/i });
    await user.click(saveButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());
    expect(capturedBody).toMatchObject({
      colors: expect.objectContaining({ primary: '#111827' }),
    });
  });

  it('renders the save button', async () => {
    renderPage();

    const saveButton = await screen.findByRole('button', { name: /save theming/i });
    expect(saveButton).toBeInTheDocument();
  });
});
