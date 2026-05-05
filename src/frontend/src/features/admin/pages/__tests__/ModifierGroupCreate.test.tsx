import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ModifierGroupCreate } from '../ModifierGroupCreate';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/modifier-groups/new" element={<ModifierGroupCreate />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/modifier-groups/new'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ModifierGroupCreate', () => {
  it('renders the form (group name input visible after settings load)', async () => {
    renderPage();

    // The group name input has placeholder "Group name in NL" and id="group-name-nl"
    // Wait for brand settings to load (defaultLanguage: 'nl') before the tab syncs
    const groupNameInput = await screen.findByPlaceholderText('Group name in NL');
    expect(groupNameInput).toBeInTheDocument();
  });

  it('shows validation error when no NL name entered and save clicked', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the form to be ready
    await screen.findByPlaceholderText('Group name in NL');

    // Click the submit button without entering a name
    const createButton = screen.getByRole('button', { name: /modifier-groep aanmaken/i });
    await user.click(createButton);

    // The validation error message is built as `${primaryLocale.toUpperCase()} name is required.`
    const errorMessage = await screen.findByText('NL name is required.');
    expect(errorMessage).toBeInTheDocument();
  });

  it('submits with correct payload (group NL name + at least one modifier with NL name)', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.post('/api/brands/:slug/modifier-groups', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'mg-new',
            translations: capturedBody.translations,
            modifiers: capturedBody.modifiers,
            createdAt: '2024-06-01T00:00:00Z',
            updatedAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Wait for form to be ready
    const groupNameInput = await screen.findByPlaceholderText('Group name in NL');

    // Enter a group name in NL
    await user.type(groupNameInput, 'Sauzen');

    // Enter a modifier name — the first modifier row is pre-rendered with id="modifier-0-name-nl"
    const modifierNameInput = screen.getByPlaceholderText('Modifier name in NL');
    await user.type(modifierNameInput, 'Mayonaise');

    // Submit the form
    const createButton = screen.getByRole('button', { name: /modifier-groep aanmaken/i });
    await user.click(createButton);

    await waitFor(() => expect(capturedBody).not.toBeNull());

    // Assert the POST body contains the NL group name
    const translations = capturedBody!.translations as Array<{
      languageCode: string;
      name: string;
    }>;
    expect(translations.some((t) => t.languageCode === 'nl' && t.name === 'Sauzen')).toBe(true);

    // Assert the POST body contains at least one modifier with NL name
    const modifiers = capturedBody!.modifiers as Array<{
      translations: Array<{ languageCode: string; name: string }>;
      priceAdjustment: number;
      sortOrder: number;
    }>;
    expect(modifiers.length).toBeGreaterThanOrEqual(1);
    const firstModifier = modifiers[0]!;
    expect(
      firstModifier.translations.some((t) => t.languageCode === 'nl' && t.name === 'Mayonaise'),
    ).toBe(true);
  });

  it('Cancel button navigates away (button exists and is clickable)', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the form to be ready
    await screen.findByPlaceholderText('Group name in NL');

    // Cancel button is rendered with hardcoded text "Cancel"
    const cancelButton = screen.getByRole('button', { name: /^cancel$/i });
    expect(cancelButton).toBeInTheDocument();

    // Clicking it should not throw — navigation happens internally via useNavigate
    await user.click(cancelButton);
  });
});
