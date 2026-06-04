import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ModifierGroupList } from '../ModifierGroupList';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/modifier-groups" element={<ModifierGroupList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/modifier-groups'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/modifier-groups', () =>
      HttpResponse.json([
        {
          id: 'mg-1',
          name: 'Sauzen',
          modifierCount: 3,
          productCount: 5,
          createdAt: '2024-01-01T00:00:00Z',
        },
      ]),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ModifierGroupList', () => {
  it('renders group name "Sauzen" after data loads', async () => {
    renderPage();

    const groupName = await screen.findByText('Sauzen');
    expect(groupName).toBeInTheDocument();
  });

  it('Create button is present in the document', async () => {
    renderPage();

    // Wait for data to load so the component is fully rendered
    await screen.findByText('Sauzen');

    // Button text comes from t('admin.modifierGroups.create') → "Modifier-groep aanmaken"
    const createButton = screen.getByRole('button', { name: /modifier-groep aanmaken/i });
    expect(createButton).toBeInTheDocument();
  });

  it('clicking a row navigates to the edit route', async () => {
    const user = userEvent.setup();

    // Render with an extra catch-all route so we can detect navigation
    const { container } = renderWithProviders(
      <Routes>
        <Route path="/:brandSlug/:lang/admin/modifier-groups" element={<ModifierGroupList />} />
        <Route
          path="/:brandSlug/:lang/admin/modifier-groups/:id"
          element={<div data-testid="edit-page">Edit Page</div>}
        />
      </Routes>,
      { initialEntries: ['/frietjes/nl/admin/modifier-groups'] },
    );

    // Wait for the row to appear
    await screen.findByText('Sauzen');

    // Click the row — the <tr> element containing "Sauzen"
    const row = container.querySelector('tr[style*="cursor: pointer"]')!;
    await user.click(row);

    // Navigation should render the edit page placeholder
    await waitFor(() => {
      expect(screen.getByTestId('edit-page')).toBeInTheDocument();
    });
  });

  it('delete button calls DELETE API', async () => {
    const user = userEvent.setup();

    let deleteCalled = false;
    server.use(
      http.delete('/api/brands/:slug/modifier-groups/:id', () => {
        deleteCalled = true;
        return new HttpResponse(null, { status: 204 });
      }),
    );

    // Stub window.confirm to auto-confirm without user interaction
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    renderPage();

    // Wait for the list to render
    await screen.findByText('Sauzen');

    // Delete button text: t('admin.modifierGroups.delete') → "Verwijderen"
    const deleteButton = screen.getByRole('button', { name: /verwijderen/i });
    await user.click(deleteButton);

    // confirm dialog should have been shown
    expect(confirmSpy).toHaveBeenCalled();

    // DELETE request should have been issued
    await waitFor(() => {
      expect(deleteCalled).toBe(true);
    });

    confirmSpy.mockRestore();
  });
});
