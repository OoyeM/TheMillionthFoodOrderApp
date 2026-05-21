import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import '../../../../i18n/config';
import { PosTicket } from '../PosTicket';
import type { PosOrderState } from '../../hooks/usePosOrder';

function makeState(overrides?: Partial<PosOrderState>): PosOrderState {
  return {
    items: [],
    orderType: 'Pickup',
    tableNumber: '',
    customerName: '',
    ...overrides,
  };
}

const noop = () => {};

describe('PosTicket', () => {
  it('shows empty message when there are no items', () => {
    render(
      <PosTicket
        state={makeState()}
        subtotal={0}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );
    expect(screen.getByTestId('pos-ticket')).toBeInTheDocument();
  });

  it('renders a line item with quantity and name', () => {
    const state = makeState({
      items: [
        {
          key: 'p1',
          productId: 'p1',
          productName: 'Frietje Klein',
          unitGrossPrice: 3.5,
          quantity: 2,
          selectedModifiers: [],
        },
      ],
    });

    render(
      <PosTicket
        state={state}
        subtotal={7}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );

    expect(screen.getByText('Frietje Klein')).toBeInTheDocument();
    expect(screen.getByTestId('qty-p1')).toHaveTextContent('2');
    expect(screen.getByTestId('pos-subtotal')).toHaveTextContent(/7/);
  });

  it('shows the table number input only when EatIn is selected', () => {
    const { rerender } = render(
      <PosTicket
        state={makeState({ orderType: 'Pickup' })}
        subtotal={0}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );

    expect(screen.queryByTestId('pos-table-number-input')).not.toBeInTheDocument();

    rerender(
      <PosTicket
        state={makeState({ orderType: 'EatIn' })}
        subtotal={0}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );

    expect(screen.getByTestId('pos-table-number-input')).toBeInTheDocument();
  });

  it('disables "Place order" when ticket is empty', () => {
    render(
      <PosTicket
        state={makeState()}
        subtotal={0}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );
    expect(screen.getByTestId('pos-place-order-btn')).toBeDisabled();
  });

  it('disables "Place order" when EatIn is selected but table number is empty', () => {
    const state = makeState({
      orderType: 'EatIn',
      tableNumber: '',
      items: [
        {
          key: 'p1',
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 3.5,
          quantity: 1,
          selectedModifiers: [],
        },
      ],
    });
    render(
      <PosTicket
        state={state}
        subtotal={3.5}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );
    expect(screen.getByTestId('pos-place-order-btn')).toBeDisabled();
  });

  it('enables "Place order" when EatIn and table number are both set', () => {
    const state = makeState({
      orderType: 'EatIn',
      tableNumber: 'T-5',
      items: [
        {
          key: 'p1',
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 3.5,
          quantity: 1,
          selectedModifiers: [],
        },
      ],
    });
    render(
      <PosTicket
        state={state}
        subtotal={3.5}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError={null}
      />,
    );
    expect(screen.getByTestId('pos-place-order-btn')).not.toBeDisabled();
  });

  it('calls onPlaceOrder when the button is clicked', () => {
    const handlePlace = vi.fn();
    const state = makeState({
      orderType: 'Pickup',
      items: [
        {
          key: 'p1',
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 3.5,
          quantity: 1,
          selectedModifiers: [],
        },
      ],
    });
    render(
      <PosTicket
        state={state}
        subtotal={3.5}
        dispatch={noop}
        onPlaceOrder={handlePlace}
        isSubmitting={false}
        submitError={null}
      />,
    );
    fireEvent.click(screen.getByTestId('pos-place-order-btn'));
    expect(handlePlace).toHaveBeenCalledOnce();
  });

  it('shows submit error when present', () => {
    render(
      <PosTicket
        state={makeState()}
        subtotal={0}
        dispatch={noop}
        onPlaceOrder={noop}
        isSubmitting={false}
        submitError="Er is iets misgegaan"
      />,
    );
    expect(screen.getByText('Er is iets misgegaan')).toBeInTheDocument();
  });
});
