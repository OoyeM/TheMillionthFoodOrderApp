import { describe, it, expect, beforeEach, vi } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ModifierGroupEdit } from '../ModifierGroupEdit';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Fixtures
// ---------------------------------------------------------------------------

const mockModifierGroup = {
  id: 'mg-1',
  translations: [
    { languageCode: 'nl', name: 'Sauzen' },
    { languageCode: 'fr', name: 'Sauces' },
  ],
  modifiers: [
    {
      id: 'mod-1',
      priceAdjustment: 0,
      sortOrder: 0,
      translations: [
        { languageCode: 'nl', name: 'Mayonaise' },
      ],
    },
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
        path="/:brandSlug/:lang/admin/modifier-groups/:modifierGroupId"
        element={<ModifierGroupEdit />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/modifier-groups/mg-1'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/brands/:slug/modifier-groups/:id', () =>
      HttpResponse.json(mockModifierGroup),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ModifierGroupEdit', () => {
  it('renders the edit form with fetched data populated', async () => {
    renderPage();

    // Wait for the NL name input to be populated with the fetched value
    // The input has id="group-name-nl" and placeholder="Group name in NL"
    const nlNameInput = await screen.findByDisplayValue('Sauzen');
    expect(nlNameInput).toBeInTheDocument();
  });

  it('user can edit the group name', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for form data to load
    const nlNameInput = await screen.findByDisplayValue('Sauzen');

    await user.clear(nlNameInput);
    await user.type(nlNameInput, 'Dips');

    expect(nlNameInput).toHaveValue('Dips');
  });

  it('submits the update with the edited payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/modifier-groups/:id', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          ...mockModifierGroup,
          translations: [{ languageCode: 'nl', name: 'Dips' }],
        });
      }),
    );

    renderPage();

    // Wait for form data to load
    const nlNameInput = await screen.findByDisplayValue('Sauzen');

    // Edit the group name
    await user.clear(nlNameInput);
    await user.type(nlNameInput, 'Dips');

    // Click Save Changes (hardcoded text in ModifierGroupEdit)
    const saveButton = screen.getByRole('button', { name: /save changes/i });
    await user.click(saveButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());

    // The PUT body should contain the updated NL translation
    const translations = capturedBody!.translations as Array<{
      languageCode: string;
      name: string;
    }>;
    expect(
      translations.some((t) => t.languageCode === 'nl' && t.name === 'Dips'),
    ).toBe(true);
  });

  it('delete flow — clicking Delete shows a confirmation dialog', async () => {
    const user = userEvent.setup();

    // Mock window.confirm to capture the call and return true (user confirms)
    const confirmSpy = vi.spyOn(window, 'confirm').mockReturnValue(true);

    // Mock the DELETE endpoint so the mutation can complete if confirm is accepted
    server.use(
      http.delete('/api/brands/:slug/modifier-groups/:id', () =>
        new HttpResponse(null, { status: 204 }),
      ),
    );

    renderPage();

    // Wait for the form to load before clicking Delete
    await screen.findByDisplayValue('Sauzen');

    // The page renders multiple "Verwijderen" buttons (one per modifier row plus
    // the group-level delete). The group delete button is the last one in the DOM.
    const verwijderenButtons = screen.getAllByRole('button', { name: /verwijderen/i });
    const deleteButton = verwijderenButtons[verwijderenButtons.length - 1]!;
    await user.click(deleteButton);

    // window.confirm should have been called with a message containing the group name
    expect(confirmSpy).toHaveBeenCalled();
    const confirmMessage = confirmSpy.mock.calls[0]?.[0] ?? '';
    expect(confirmMessage).toMatch(/sauzen/i);

    confirmSpy.mockRestore();
  });
});
