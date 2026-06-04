import { describe, it, expect, beforeEach } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { PlatformAdminList } from '../PlatformAdminList';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/platform-admins" element={<PlatformAdminList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/platform-admins'] },
  );
}

// ---------------------------------------------------------------------------
// Common handler overrides — applied before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  server.use(
    http.get('/api/platform-admins', () =>
      HttpResponse.json([
        {
          id: 'pa-1',
          email: 'admin@platform.dev',
          displayName: 'Platform Admin',
          isPlatformAdmin: true,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-01T00:00:00Z',
        },
      ]),
    ),
  );
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('PlatformAdminList', () => {
  it('renders admin list with Platform Admin visible after load', async () => {
    renderPage();

    const nameCells = await screen.findAllByText('Platform Admin');
    expect(nameCells.length).toBeGreaterThan(0);
  });

  it('deactivate button calls POST /api/platform-admins/:id/deactivate', async () => {
    const user = userEvent.setup();

    let deactivateCalled = false;
    let capturedId: string | undefined;

    server.use(
      http.post('/api/platform-admins/:id/deactivate', ({ params }) => {
        deactivateCalled = true;
        capturedId = params.id as string;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderPage();

    // Wait for the admin row to appear
    await screen.findAllByText('Platform Admin');

    // Click the deactivate button on the row — renders "Deactiveren" (NL)
    const deactivateBtn = screen.getByRole('button', { name: /deactiveren/i });
    await user.click(deactivateBtn);

    // A confirmation dialog appears — click the confirm (second "Deactiveren") button
    const confirmBtns = await screen.findAllByRole('button', { name: /deactiveren/i });
    // The confirm button inside the dialog is the last one
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- findAllByRole guarantees a non-empty array, so the last element exists
    await user.click(confirmBtns[confirmBtns.length - 1]!);

    await waitFor(() => { expect(deactivateCalled).toBe(true); });
    expect(capturedId).toBe('pa-1');
  });

  it('invite form submits with correct email + displayName payload', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.post('/api/platform-admins', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'pa-new',
            email: capturedBody.email,
            displayName: capturedBody.displayName,
            isPlatformAdmin: true,
            createdAt: '2024-06-01T00:00:00Z',
            updatedAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Wait for page to be ready
    await screen.findAllByText('Platform Admin');

    // Open the invite form
    const inviteOpenBtn = screen.getByRole('button', { name: /beheerder uitnodigen/i });
    await user.click(inviteOpenBtn);

    // The form has two inputs: email (index 0) and displayName (index 1).
    // Labels are not associated via htmlFor so we query by role index.
    const textboxes = screen.getAllByRole('textbox');
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the invite form renders exactly two textboxes (email, displayName)
    const emailInput = textboxes[0]!;
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the invite form renders exactly two textboxes (email, displayName)
    const displayNameInput = textboxes[1]!;

    await user.type(emailInput, 'new@platform.dev');
    await user.type(displayNameInput, 'New Admin');

    // Submit — button text is "Beheerder uitnodigen" inside the form
    const submitBtns = screen.getAllByRole('button', { name: /beheerder uitnodigen/i });
    // The submit button inside the form is the last rendered one
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- getAllByRole guarantees a non-empty array, so the last element exists
    await user.click(submitBtns[submitBtns.length - 1]!);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above asserts capturedBody is non-null
    expect(capturedBody!.email).toBe('new@platform.dev');
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above asserts capturedBody is non-null
    expect(capturedBody!.displayName).toBe('New Admin');
  });

  it('invite form shows validation error when email is empty', async () => {
    const user = userEvent.setup();

    renderPage();

    // Wait for page to be ready
    await screen.findAllByText('Platform Admin');

    // Open the invite form
    const inviteOpenBtn = screen.getByRole('button', { name: /beheerder uitnodigen/i });
    await user.click(inviteOpenBtn);

    // Fill in displayName but leave email empty.
    // Labels are not associated via htmlFor; query by role index (displayName is index 1).
    const textboxes = screen.getAllByRole('textbox');
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the invite form renders exactly two textboxes (email, displayName)
    const displayNameInput = textboxes[1]!;
    await user.type(displayNameInput, 'New Admin');

    // Submit without filling in email
    const submitBtns = screen.getAllByRole('button', { name: /beheerder uitnodigen/i });
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- getAllByRole guarantees a non-empty array, so the last element exists
    await user.click(submitBtns[submitBtns.length - 1]!);

    // Validation error: source builds "E-mailadres is required."
    const emailError = await screen.findByText('E-mailadres is required.');
    expect(emailError).toBeInTheDocument();
  });
});
