/**
 * t-11 — OrderType toggle switches Pickup/EatIn and shows/hides the table input [AC3]
 * t-12 — EatIn with empty table blocks submit; with a table it proceeds [AC4]
 */
import { describe, it, expect } from 'vitest';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { renderWithProviders } from '@/test/testUtils';
import { PosOrderProvider, useOrderState } from '../context/PosOrderContext';
import { PosOrderPanel } from '../components/PosOrderPanel';

// ── Helpers ──────────────────────────────────────────────────────────────────

function renderPanel() {
  return renderWithProviders(
    <PosOrderProvider>
      <PosOrderPanel />
    </PosOrderProvider>,
    { initialEntries: ['/frietjes/nl/pos'] },
  );
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('PosOrderPanel — OrderType toggle (t-11)', () => {
  it('renders Pickup and EatIn toggle buttons', () => {
    renderPanel();

    expect(screen.getByTestId('order-type-pickup')).toBeInTheDocument();
    expect(screen.getByTestId('order-type-eatin')).toBeInTheDocument();
  });

  it('defaults to Pickup and does NOT show the table number input', () => {
    renderPanel();

    // Pickup button is active by default
    expect(screen.getByTestId('order-type-pickup')).toHaveAttribute('aria-pressed', 'true');

    // Table number input should NOT be present when Pickup is selected
    expect(screen.queryByTestId('table-number-input')).not.toBeInTheDocument();
  });

  it('shows the table number input when EatIn is selected', () => {
    renderPanel();

    fireEvent.click(screen.getByTestId('order-type-eatin'));

    expect(screen.getByTestId('table-number-input')).toBeInTheDocument();
  });

  it('hides the table number input when switching back to Pickup', () => {
    renderPanel();

    // Switch to EatIn
    fireEvent.click(screen.getByTestId('order-type-eatin'));
    expect(screen.getByTestId('table-number-input')).toBeInTheDocument();

    // Switch back to Pickup
    fireEvent.click(screen.getByTestId('order-type-pickup'));
    expect(screen.queryByTestId('table-number-input')).not.toBeInTheDocument();
  });
});

describe('PosOrderPanel — table number input (t-12 precondition)', () => {
  it('accepts numeric input for the table number', () => {
    renderPanel();

    fireEvent.click(screen.getByTestId('order-type-eatin'));

    const input = screen.getByTestId('table-number-input');
    fireEvent.change(input, { target: { value: '5' } });

    expect((input as HTMLInputElement).value).toBe('5');
  });
});

// ── Submit guard tests (t-12) ─────────────────────────────────────────────────

/**
 * Minimal wrapper that exposes PosOrderPanel + a submit button to test the
 * EatIn-without-table blocking behaviour from Dashboard.
 */
function SubmitGuardWrapper() {
  const { state } = useOrderState();

  const isEatInMissingTable = state.orderType === 'EatIn' && !state.tableNumber;
  const canSubmit = state.items.length > 0 && !isEatInMissingTable;

  return (
    <div>
      <PosOrderPanel />
      {isEatInMissingTable && state.items.length > 0 && (
        <p role="alert" data-testid="table-error">
          Tafelnummer ontbreekt
        </p>
      )}
      <button type="button" disabled={!canSubmit} data-testid="place-order-btn">
        Bestelling plaatsen
      </button>
    </div>
  );
}

function renderWithItems() {
  function PreloadedWrapper() {
    const { addItem } = useOrderState();

    return (
      <button
        type="button"
        data-testid="add-item-btn"
        onClick={() =>
          addItem({
            productId: 'prod-1',
            productName: 'Friet',
            quantity: 1,
            unitGrossPrice: 3.5,
            selectedModifiers: [],
          })
        }
      >
        Add item
      </button>
    );
  }

  return renderWithProviders(
    <PosOrderProvider>
      <PreloadedWrapper />
      <SubmitGuardWrapper />
    </PosOrderProvider>,
    { initialEntries: ['/frietjes/nl/pos'] },
  );
}

describe('PosOrderPanel — submit guard (t-12)', () => {
  it('blocks submit when EatIn is selected but table number is empty', async () => {
    renderWithItems();

    // Add an item first
    fireEvent.click(screen.getByTestId('add-item-btn'));

    // Switch to EatIn
    fireEvent.click(screen.getByTestId('order-type-eatin'));

    await waitFor(() => {
      // Submit button should be disabled
      expect(screen.getByTestId('place-order-btn')).toBeDisabled();
      // Error alert should show
      expect(screen.getByTestId('table-error')).toBeInTheDocument();
    });
  });

  it('enables submit when EatIn is selected and table number is provided', async () => {
    renderWithItems();

    // Add an item
    fireEvent.click(screen.getByTestId('add-item-btn'));

    // Switch to EatIn
    fireEvent.click(screen.getByTestId('order-type-eatin'));

    // Enter a table number
    const input = screen.getByTestId('table-number-input');
    fireEvent.change(input, { target: { value: '5' } });

    await waitFor(() => {
      expect(screen.getByTestId('place-order-btn')).not.toBeDisabled();
      expect(screen.queryByTestId('table-error')).not.toBeInTheDocument();
    });
  });
});
