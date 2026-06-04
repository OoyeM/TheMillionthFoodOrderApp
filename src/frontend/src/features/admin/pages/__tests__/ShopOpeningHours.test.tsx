import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { ShopOpeningHours } from '../ShopOpeningHours';
import '../../../../i18n/config'; // Initialize i18n synchronously (resources are inlined)

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route
        path="/:brandSlug/:lang/admin/shops/:shopId/opening-hours"
        element={<ShopOpeningHours />}
      />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/shops/shop-1/opening-hours'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ShopOpeningHours', () => {
  it('renders the existing Monday block (openTime 09:00, closeTime 18:00) after data loads', async () => {
    renderPage();

    // Wait for Monday label to appear (Dutch: "Maandag")
    await screen.findByText('Maandag');

    // The block's time inputs should carry the loaded values
    const timeInputs = screen.getAllByDisplayValue('09:00');
    expect(timeInputs.length).toBeGreaterThanOrEqual(1);

    const closeInputs = screen.getAllByDisplayValue('18:00');
    expect(closeInputs.length).toBeGreaterThanOrEqual(1);
  });

  it('Add Time Block button adds a new row to the form', async () => {
    const user = userEvent.setup();
    renderPage();

    // Wait for the page to load
    await screen.findByText('Maandag');

    // There is one existing block for Monday — count initial "Verwijderen" buttons
    const removeButtonsBefore = screen.getAllByRole('button', { name: /verwijderen/i });
    const countBefore = removeButtonsBefore.length;

    // Click the first "Tijdblok toevoegen" button (Monday's add button)
    const addButtons = screen.getAllByRole('button', { name: /tijdblok toevoegen/i });
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- getAllByRole guarantees a non-empty array, so the first element exists
    await user.click(addButtons[0]!);

    // A new row should have been added — one more remove button
    await waitFor(() => {
      const removeButtonsAfter = screen.getAllByRole('button', { name: /verwijderen/i });
      expect(removeButtonsAfter.length).toBe(countBefore + 1);
    });
  });

  it('Save submits the blocks to the PUT endpoint', async () => {
    const user = userEvent.setup();

    let capturedBody: { timeBlocks: unknown[] } | null = null;
    server.use(
      http.put('/api/brands/:slug/shops/:shopId/opening-hours', async ({ request }) => {
        capturedBody = (await request.json()) as { timeBlocks: unknown[] };
        return HttpResponse.json({ timeBlocks: capturedBody.timeBlocks });
      }),
    );

    renderPage();

    // Wait for page to load with the existing block
    await screen.findByText('Maandag');

    // Click Save
    const saveButton = screen.getByRole('button', { name: /openingstijden opslaan/i });
    await user.click(saveButton);

    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    // The PUT body should contain the Monday block
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above asserts capturedBody is non-null
    expect(capturedBody!.timeBlocks).toHaveLength(1);
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above asserts capturedBody is non-null
    const block = capturedBody!.timeBlocks[0] as {
      dayOfWeek: number;
      openTime: string;
      closeTime: string;
    };
    expect(block.dayOfWeek).toBe(1);
    expect(block.openTime).toBe('09:00');
    expect(block.closeTime).toBe('18:00');
  });

  it('validation error appears when a block has closeTime <= openTime', async () => {
    const user = userEvent.setup();

    renderPage();

    // Wait for page to load with the existing Monday block
    await screen.findByText('Maandag');

    // The existing block has openTime=09:00, closeTime=18:00 (valid).
    // Change closeTime to a value before openTime to trigger validation.
    const closeTimeInputs = screen.getAllByDisplayValue('18:00');
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- getAllByDisplayValue guarantees a non-empty array, so the first element exists
    await user.clear(closeTimeInputs[0]!);
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- getAllByDisplayValue guarantees a non-empty array, so the first element exists
    await user.type(closeTimeInputs[0]!, '08:00');

    // Click Save — should not call PUT, should show validation error instead
    const saveButton = screen.getByRole('button', { name: /openingstijden opslaan/i });
    await user.click(saveButton);

    // Validation error message is hardcoded in the component
    await screen.findByText('Close time must be after open time.');
  });
});
