import { describe, it, expect } from 'vitest';
import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Routes, Route } from 'react-router-dom';
import { http, HttpResponse } from 'msw';

import { renderWithProviders } from '../../../../test/testUtils';
import { server } from '../../../../test/msw/server';
import { TaxConfiguration } from '../TaxConfiguration';
import '../../../../i18n/config';

// ---------------------------------------------------------------------------
// Render helper
// ---------------------------------------------------------------------------

function renderPage() {
  return renderWithProviders(
    <Routes>
      <Route path="/:brandSlug/:lang/admin/tax-configuration" element={<TaxConfiguration />} />
    </Routes>,
    { initialEntries: ['/frietjes/nl/admin/tax-configuration'] },
  );
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('TaxConfiguration', () => {
  it('renders the fetched VAT rates (6% Takeaway, 21% EatIn) after data loads', async () => {
    const { container } = renderPage();

    // Wait for loading to finish — the page title appears once data is loaded
    await screen.findByRole('heading', { name: /btw-configuratie/i });

    // Takeaway rate input has id="rate-Takeaway"
    const takeawayInput = container.querySelector<HTMLInputElement>('#rate-Takeaway');
    expect(takeawayInput).not.toBeNull();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- expect.not.toBeNull above asserts takeawayInput is non-null
    expect(Number(takeawayInput!.value)).toBe(6);

    // EatIn rate input has id="rate-EatIn"
    const eatInInput = container.querySelector<HTMLInputElement>('#rate-EatIn');
    expect(eatInInput).not.toBeNull();
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- expect.not.toBeNull above asserts eatInInput is non-null
    expect(Number(eatInInput!.value)).toBe(21);
  });

  it('user can change a rate value in the input', async () => {
    const user = userEvent.setup();
    const { container } = renderPage();

    // Wait for the form to be ready
    await screen.findByRole('heading', { name: /btw-configuratie/i });

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the rate input renders once the page has loaded (awaited above)
    const takeawayInput = container.querySelector<HTMLInputElement>('#rate-Takeaway')!;

    // Clear and type a new value
    await user.clear(takeawayInput);
    await user.type(takeawayInput, '12');

    expect(Number(takeawayInput.value)).toBe(12);
  });

  it('Save submits updated rates to PUT endpoint', async () => {
    const user = userEvent.setup();

    let capturedBody: Record<string, unknown> | null = null;
    server.use(
      http.put('/api/brands/:slug/tax-configuration', async ({ request }) => {
        capturedBody = (await request.json()) as Record<string, unknown>;
        return HttpResponse.json({
          id: 'tax-1',
          vatRates: capturedBody.vatRates,
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-06-01T00:00:00Z',
        });
      }),
    );

    const { container } = renderPage();

    // Wait for form to load
    await screen.findByRole('heading', { name: /btw-configuratie/i });

    // Change the Takeaway rate to 9
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the rate input renders once the page has loaded (awaited above)
    const takeawayInput = container.querySelector<HTMLInputElement>('#rate-Takeaway')!;
    await user.clear(takeawayInput);
    await user.type(takeawayInput, '9');

    // Click the save button
    const saveButton = screen.getByRole('button', { name: /^opslaan$/i });
    await user.click(saveButton);

    // Wait for the PUT to be captured
    await waitFor(() => { expect(capturedBody).not.toBeNull(); });

    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- waitFor above asserts capturedBody is non-null
    const vatRates = capturedBody!.vatRates as {
      consumptionMode: string;
      ratePercentage: number;
    }[];

    const takeawayRate = vatRates.find((r) => r.consumptionMode === 'Takeaway');
    expect(takeawayRate?.ratePercentage).toBe(9);

    const eatInRate = vatRates.find((r) => r.consumptionMode === 'EatIn');
    expect(eatInRate?.ratePercentage).toBe(21);
  });

  it('entering a gross amount displays the VAT breakdown result', async () => {
    const user = userEvent.setup();
    const { container } = renderPage();

    // Wait for page to be ready
    await screen.findByRole('heading', { name: /btw-configuratie/i });

    // The gross amount input starts with "10.00" — breakdown should already be visible
    // Verify the breakdown section is present by checking for "Netto (excl. BTW)"
    const netAmountLabel = await screen.findByText(/netto \(excl\. btw\)/i);
    expect(netAmountLabel).toBeInTheDocument();

    // Change the gross amount to a different value to confirm reactivity
    // id="calc-gross" is the input for the gross amount in the example calculation section
    // eslint-disable-next-line @typescript-eslint/no-non-null-assertion -- the gross input renders once the page has loaded (awaited above)
    const grossInput = container.querySelector<HTMLInputElement>('#calc-gross')!;
    await user.clear(grossInput);
    await user.type(grossInput, '21');

    // Default calcMode is 'Takeaway' (6%): net = round(21 / 1.06 * 100) / 100 = 19.81
    // The breakdown renders gross amount row as "€ 21.00"
    await waitFor(() => {
      // Gross amount row shows the entered amount
      const grossAmountText = screen.getAllByText(/€\s*21\.00/i);
      expect(grossAmountText.length).toBeGreaterThan(0);
    });
  });
});
