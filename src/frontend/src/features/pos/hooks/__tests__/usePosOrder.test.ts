import { renderHook, act } from '@testing-library/react';
import { describe, it, expect } from 'vitest';
import { usePosOrder } from '../usePosOrder';

describe('usePosOrder', () => {
  it('starts with an empty ticket', () => {
    const { result } = renderHook(() => usePosOrder());
    expect(result.current.state.items).toHaveLength(0);
    expect(result.current.state.orderType).toBe('Pickup');
    expect(result.current.state.tableNumber).toBe('');
    expect(result.current.subtotal).toBe(0);
  });

  it('ADD_ITEM adds a product to the ticket', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: {
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 3.5,
          selectedModifiers: [],
        },
      });
    });

    expect(result.current.state.items).toHaveLength(1);
    expect(result.current.state.items[0]?.productName).toBe('Frietje');
    expect(result.current.state.items[0]?.quantity).toBe(1);
    expect(result.current.subtotal).toBe(3.5);
  });

  it('ADD_ITEM merges quantity when the same product+modifiers is added twice', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: { productId: 'p1', productName: 'Frietje', unitGrossPrice: 3.5, selectedModifiers: [] },
      });
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: { productId: 'p1', productName: 'Frietje', unitGrossPrice: 3.5, selectedModifiers: [] },
      });
    });

    expect(result.current.state.items).toHaveLength(1);
    expect(result.current.state.items[0]?.quantity).toBe(2);
    expect(result.current.subtotal).toBe(7);
  });

  it('ADD_ITEM creates separate lines for same product with different modifiers', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: {
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 3.5,
          selectedModifiers: [],
        },
      });
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: {
          productId: 'p1',
          productName: 'Frietje',
          unitGrossPrice: 4.0,
          selectedModifiers: [{ modifierId: 'm1', modifierName: 'Extra groot', priceAdjustment: 0.5 }],
        },
      });
    });

    expect(result.current.state.items).toHaveLength(2);
  });

  it('UPDATE_QUANTITY removes item when quantity is set to 0', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: { productId: 'p1', productName: 'Frietje', unitGrossPrice: 3.5, selectedModifiers: [] },
      });
    });
    const key = result.current.state.items[0]!.key;
    act(() => {
      result.current.dispatch({ type: 'UPDATE_QUANTITY', payload: { key, quantity: 0 } });
    });

    expect(result.current.state.items).toHaveLength(0);
  });

  it('REMOVE_ITEM removes item by key', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: { productId: 'p1', productName: 'Frietje', unitGrossPrice: 3.5, selectedModifiers: [] },
      });
    });
    const key = result.current.state.items[0]!.key;
    act(() => {
      result.current.dispatch({ type: 'REMOVE_ITEM', payload: { key } });
    });

    expect(result.current.state.items).toHaveLength(0);
  });

  it('SET_ORDER_TYPE updates the order type', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({ type: 'SET_ORDER_TYPE', payload: { orderType: 'EatIn' } });
    });

    expect(result.current.state.orderType).toBe('EatIn');
  });

  it('SET_ORDER_TYPE clears table number when switching away from EatIn', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({ type: 'SET_ORDER_TYPE', payload: { orderType: 'EatIn' } });
      result.current.dispatch({ type: 'SET_TABLE_NUMBER', payload: { tableNumber: 'T-12' } });
    });
    expect(result.current.state.tableNumber).toBe('T-12');

    act(() => {
      result.current.dispatch({ type: 'SET_ORDER_TYPE', payload: { orderType: 'Pickup' } });
    });

    expect(result.current.state.tableNumber).toBe('');
  });

  it('SET_TABLE_NUMBER updates the table number', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({ type: 'SET_TABLE_NUMBER', payload: { tableNumber: 'Bar-3' } });
    });

    expect(result.current.state.tableNumber).toBe('Bar-3');
  });

  it('SET_CUSTOMER_NAME updates the customer name', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({ type: 'SET_CUSTOMER_NAME', payload: { customerName: 'Jan' } });
    });

    expect(result.current.state.customerName).toBe('Jan');
  });

  it('RESET returns to initial state', () => {
    const { result } = renderHook(() => usePosOrder());
    act(() => {
      result.current.dispatch({
        type: 'ADD_ITEM',
        payload: { productId: 'p1', productName: 'Frietje', unitGrossPrice: 3.5, selectedModifiers: [] },
      });
      result.current.dispatch({ type: 'SET_ORDER_TYPE', payload: { orderType: 'EatIn' } });
      result.current.dispatch({ type: 'SET_TABLE_NUMBER', payload: { tableNumber: 'T-1' } });
      result.current.dispatch({ type: 'RESET' });
    });

    expect(result.current.state.items).toHaveLength(0);
    expect(result.current.state.orderType).toBe('Pickup');
    expect(result.current.state.tableNumber).toBe('');
    expect(result.current.subtotal).toBe(0);
  });
});
