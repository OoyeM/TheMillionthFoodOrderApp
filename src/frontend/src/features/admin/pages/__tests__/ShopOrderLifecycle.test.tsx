import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ShopOrderLifecycle } from '../ShopOrderLifecycle';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/shops/:shopId/order-lifecycle"
        element={<ShopOrderLifecycle />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/shops/shop-1/order-lifecycle'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopOrderLifecycle', () => {
  it('renders the fetched status "New" after data loads', async () => {
    renderPage();

    // The status name "New" should appear in the form once data has loaded.
    // It appears both in the visual flow badge and as an input value.
    const nameInput = await screen.findByDisplayValue('New');
    expect(nameInput).toBeInTheDocument();
  });

  it('Add Status button adds a new row to the form', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for initial data to load
    await screen.findByDisplayValue('New');

    // Count existing status name inputs before adding
    const inputsBefore = screen.getAllByPlaceholderText(/statusnaam/i);
    const countBefore = inputsBefore.length;

    // Click the Add Status button (label: "+ Status toevoegen")
    const addButton = screen.getByRole('button', { name: /status toevoegen/i });
    await user.click(addButton);

    // There should now be one more status input row
    await waitFor(() => {
      const inputsAfter = screen.getAllByPlaceholderText(/statusnaam/i);
      expect(inputsAfter.length).toBe(countBefore + 1);
    });
  });

  it('Save button submits the lifecycle to the PUT endpoint', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put(
        '/api/brands/:slug/shops/:shopId/order-lifecycle',
        async ({ request }) => {
          capturedBody = (await request.json()) as Record<string, unknown>;
          return HttpResponse.json({
            shopId: 'shop-1',
            statuses: capturedBody.statuses,
            transitions: capturedBody.transitions,
          });
        },
      ),
    );

    renderPage();

    // Wait for the fetched status to appear
    await screen.findByDisplayValue('New');

    // The default fixture only has 1 status; validation requires >= 2 and >= 1 terminal.
    // Add a second status.
    const addButton = screen.getByRole('button', { name: /status toevoegen/i });
    await user.click(addButton);

    // Fill in the new (empty) status name
    const emptyInputs = screen.getAllByPlaceholderText(/statusnaam/i);
    const newInput = emptyInputs.find(
      (el) => (el as HTMLInputElement).value === '',
    ) as HTMLInputElement;
    await user.type(newInput, 'Delivered');

    // Mark the new status as terminal via its checkbox
    // The terminal checkboxes are associated with rows; find the one in the new row
    const terminalCheckboxes = screen.getAllByRole('checkbox');
    // The last checkbox corresponds to the newly added row
    const lastCheckbox = terminalCheckboxes[terminalCheckboxes.length - 1]!;
    await user.click(lastCheckbox);

    // Click the Save button ("Levenscyclus opslaan")
    const saveButton = screen.getByRole('button', { name: /levenscyclus opslaan/i });
    await user.click(saveButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    // The PUT body should contain statuses
    const statuses = capturedBody!.statuses as { name: string }[];
    expect(statuses.some((s) => s.name === 'New')).toBe(true);
    expect(statuses.some((s) => s.name === 'Delivered')).toBe(true);
  });

  it('Reset button shows a confirm dialog and calls the reset endpoint', async () => {
    const user = userEvent.setup();

    let resetCalled = false;
    server.use(
      http.post(
        '/api/brands/:slug/shops/:shopId/order-lifecycle/reset',
        ({ params }) => {
          resetCalled = true;
          return HttpResponse.json({
            shopId: params.shopId,
            statuses: [],
            transitions: [],
          });
        },
      ),
    );

    renderPage();

    // Wait for the fetched status to appear
    await screen.findByDisplayValue('New');

    // Click the "Standaard herstellen" button — this should open the confirm dialog
    const resetButton = screen.getByRole('button', { name: /standaard herstellen/i });
    await user.click(resetButton);

    // The confirmation dialog should now be visible
    const confirmTitle = await screen.findByText(/standaard herstellen\?/i);
    expect(confirmTitle).toBeInTheDocument();

    // Click the confirm ("Ja, herstellen") button inside the dialog
    const confirmResetButton = screen.getByRole('button', { name: /ja, herstellen/i });
    await user.click(confirmResetButton);

    // The reset endpoint should have been called
    await waitFor(() => { expect(resetCalled).toBe(true); });
  });
});
