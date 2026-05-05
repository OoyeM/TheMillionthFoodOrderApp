import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { StaffList } from '../StaffList';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/staff" element={<StaffList />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/staff'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('StaffList', () => {
  it('renders staff member after data loads', async () => {
    renderPage();

    // Default MSW handler returns a staff member with displayName 'Staff Member'
    const staffName = await screen.findByText('Staff Member');
    expect(staffName).toBeInTheDocument();
  });

  it('deactivate button calls POST /api/brands/:slug/staff/:roleId/deactivate', async () => {
    const user = userEvent.setup();

    let deactivateCalled = false;
    let capturedRoleId: string | undefined;

    server.use(
      http.post('/api/brands/:slug/staff/:roleId/deactivate', ({ params }) => {
        deactivateCalled = true;
        capturedRoleId = params.roleId as string;
        return new HttpResponse(null, { status: 200 });
      }),
    );

    renderPage();

    // Wait for staff member to appear
    await screen.findByText('Staff Member');

    // Click the Deactivate button on the staff row — i18n key: admin.staff.deactivate = "Deactiveren"
    const deactivateBtn = screen.getByRole('button', { name: /deactiveren/i });
    await user.click(deactivateBtn);

    // A confirmation dialog appears — click the confirm button (also "Deactiveren")
    const confirmButtons = screen.getAllByRole('button', { name: /deactiveren/i });
    // The last "Deactiveren" button is inside the dialog
    const confirmBtn = confirmButtons[confirmButtons.length - 1]!;
    await user.click(confirmBtn);

    await waitFor(() => expect(deactivateCalled).toBe(true));
    expect(capturedRoleId).toBe('role-1');
  });

  it('invite form submits with BrandAdmin role (email + displayName)', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;

    server.use(
      http.post('/api/brands/:slug/staff', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json(
          {
            id: 'staff-new',
            email: capturedBody.email,
            displayName: capturedBody.displayName,
            roleId: 'role-new',
            role: capturedBody.role,
            shopId: null,
            shopName: null,
            createdAt: '2024-06-01T00:00:00Z',
          },
          { status: 201 },
        );
      }),
    );

    renderPage();

    // Wait for the page to load
    await screen.findByText('Staff Member');

    // Click the invite button — label: "+ Medewerker uitnodigen"
    const inviteBtn = screen.getByRole('button', { name: /medewerker uitnodigen/i });
    await user.click(inviteBtn);

    // The form has two textboxes: first is email (type="email"), second is displayName (type="text")
    const textboxes = screen.getAllByRole('textbox');
    const emailInput = textboxes[0]!;
    const displayNameInput = textboxes[1]!;

    await user.type(emailInput, 'new@frietjes.be');
    await user.type(displayNameInput, 'New Staff');

    // Role defaults to BrandAdmin — no need to change it
    // Shop dropdown should NOT be visible for BrandAdmin (only one combobox: the role select)
    expect(screen.getAllByRole('combobox')).toHaveLength(1);

    // Submit the form — the submit button inside the form
    const submitBtn = screen.getByRole('button', { name: /^medewerker uitnodigen$/i });
    await user.click(submitBtn);

    await waitFor(() => expect(capturedBody).not.toBeNull());

    expect(capturedBody!.email).toBe('new@frietjes.be');
    expect(capturedBody!.displayName).toBe('New Staff');
    // BrandAdmin = 0
    expect(capturedBody!.role).toBe(0);
    expect(capturedBody!.shopId).toBeNull();
  });

  it('shop dropdown appears when role is changed to CounterStaff', async () => {
    const user = userEvent.setup();

    renderPage();

    // Wait for the page to load
    await screen.findByText('Staff Member');

    // Open invite form
    const inviteBtn = screen.getByRole('button', { name: /medewerker uitnodigen/i });
    await user.click(inviteBtn);

    // Initially only one combobox (the role select) — no shop dropdown for BrandAdmin
    expect(screen.getAllByRole('combobox')).toHaveLength(1);

    // Change role to CounterStaff — the only combobox is the role select
    const roleSelect = screen.getByRole('combobox');
    await user.selectOptions(roleSelect, 'CounterStaff');

    // Shop dropdown should now be visible — two comboboxes total
    await waitFor(() => {
      expect(screen.getAllByRole('combobox')).toHaveLength(2);
    });

    // The shop 'Gent Centrum' from the default MSW handler should be an option
    expect(screen.getByRole('option', { name: 'Gent Centrum' })).toBeInTheDocument();
  });
});
