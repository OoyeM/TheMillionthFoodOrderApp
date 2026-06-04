import {
  createContext,
  useContext,
  useReducer,
  useCallback,
  useMemo,
  type ReactNode,
} from 'react';
import type { CartItem, CartModifier } from '@features/storefront/context/CartContext';
import type { OrderType, PaymentMethod } from '@api/orders';

// Re-export CartItem and CartModifier so POS components can import from one place
export type { CartItem, CartModifier };

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface PosOrderState {
  items: CartItem[];
  orderType: OrderType;
  tableNumber: number | undefined;
  paymentMethod: PaymentMethod;
}

export interface PosOrderTotals {
  subtotalGross: number;
  vatPercent: number;
  vatAmount: number;
  totalGross: number;
}

interface PosOrderContextValue {
  state: PosOrderState;
  totals: PosOrderTotals;
  addItem: (item: CartItem) => void;
  removeItem: (productId: string, modifiers: CartModifier[]) => void;
  updateQuantity: (productId: string, modifiers: CartModifier[], quantity: number) => void;
  clearOrder: () => void;
  setOrderType: (orderType: OrderType) => void;
  setTableNumber: (tableNumber: number | undefined) => void;
  setPaymentMethod: (paymentMethod: PaymentMethod) => void;
  getModifierKey: (modifiers: CartModifier[]) => string;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Creates a stable deduplication key from selected modifiers.
 * Same product + same modifiers = same line item.
 */
// eslint-disable-next-line react-refresh/only-export-components -- HMR boundary rule; helper co-located with the context it serves
export function modifierKey(selectedModifiers: CartModifier[]): string {
  return selectedModifiers
    .map((m) => m.modifierId)
    .sort()
    .join(',');
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------

type PosOrderAction =
  | { type: 'ADD_ITEM'; payload: CartItem }
  | { type: 'REMOVE_ITEM'; payload: { productId: string; modifierKey: string } }
  | { type: 'UPDATE_QUANTITY'; payload: { productId: string; modifierKey: string; quantity: number } }
  | { type: 'CLEAR_ORDER' }
  | { type: 'SET_ORDER_TYPE'; payload: OrderType }
  | { type: 'SET_TABLE_NUMBER'; payload: number | undefined }
  | { type: 'SET_PAYMENT_METHOD'; payload: PaymentMethod };

// ---------------------------------------------------------------------------
// Reducer
// ---------------------------------------------------------------------------

function posOrderReducer(state: PosOrderState, action: PosOrderAction): PosOrderState {
  switch (action.type) {
    case 'ADD_ITEM': {
      const newItemKey = modifierKey(action.payload.selectedModifiers);
      const existingIndex = state.items.findIndex(
        (item) =>
          item.productId === action.payload.productId &&
          modifierKey(item.selectedModifiers) === newItemKey,
      );

      if (existingIndex >= 0) {
        const updatedItems = state.items.map((item, idx) =>
          idx === existingIndex
            ? { ...item, quantity: item.quantity + action.payload.quantity }
            : item,
        );
        return { ...state, items: updatedItems };
      }

      return { ...state, items: [...state.items, action.payload] };
    }

    case 'REMOVE_ITEM':
      return {
        ...state,
        items: state.items.filter(
          (item) =>
            !(
              item.productId === action.payload.productId &&
              modifierKey(item.selectedModifiers) === action.payload.modifierKey
            ),
        ),
      };

    case 'UPDATE_QUANTITY':
      if (action.payload.quantity <= 0) {
        return {
          ...state,
          items: state.items.filter(
            (item) =>
              !(
                item.productId === action.payload.productId &&
                modifierKey(item.selectedModifiers) === action.payload.modifierKey
              ),
          ),
        };
      }
      return {
        ...state,
        items: state.items.map((item) =>
          item.productId === action.payload.productId &&
          modifierKey(item.selectedModifiers) === action.payload.modifierKey
            ? { ...item, quantity: action.payload.quantity }
            : item,
        ),
      };

    case 'CLEAR_ORDER':
      return {
        items: [],
        orderType: state.orderType,
        tableNumber: undefined,
        paymentMethod: state.paymentMethod,
      } satisfies PosOrderState;

    case 'SET_ORDER_TYPE': {
      // Clear table number when switching away from EatIn
      const nextTableNumber =
        action.payload === 'EatIn' ? state.tableNumber : undefined;
      return {
        ...state,
        orderType: action.payload,
        tableNumber: nextTableNumber,
      } satisfies PosOrderState;
    }

    case 'SET_TABLE_NUMBER':
      return { ...state, tableNumber: action.payload };

    case 'SET_PAYMENT_METHOD':
      return { ...state, paymentMethod: action.payload };

    default:
      return state;
  }
}

// ---------------------------------------------------------------------------
// Context
// ---------------------------------------------------------------------------

const PosOrderContext = createContext<PosOrderContextValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

interface PosOrderProviderProps {
  children: ReactNode;
  /** Initial payment method. Defaults to CashAtPickup (POS default). */
  initialPaymentMethod?: PaymentMethod;
  /** Initial order type. Defaults to Pickup. */
  initialOrderType?: OrderType;
}

export function PosOrderProvider({
  children,
  initialPaymentMethod = 'CashAtPickup',
  initialOrderType = 'Pickup',
}: PosOrderProviderProps) {
  const initialState: PosOrderState = {
    items: [],
    orderType: initialOrderType,
    tableNumber: undefined,
    paymentMethod: initialPaymentMethod,
  };

  const [state, dispatch] = useReducer(posOrderReducer, initialState);

  const addItem = useCallback((item: CartItem) => {
    dispatch({ type: 'ADD_ITEM', payload: item });
  }, []);

  const removeItem = useCallback((productId: string, modifiers: CartModifier[]) => {
    dispatch({
      type: 'REMOVE_ITEM',
      payload: { productId, modifierKey: modifierKey(modifiers) },
    });
  }, []);

  const updateQuantity = useCallback(
    (productId: string, modifiers: CartModifier[], quantity: number) => {
      dispatch({
        type: 'UPDATE_QUANTITY',
        payload: { productId, modifierKey: modifierKey(modifiers), quantity },
      });
    },
    [],
  );

  const clearOrder = useCallback(() => {
    dispatch({ type: 'CLEAR_ORDER' });
  }, []);

  const setOrderType = useCallback((orderType: OrderType) => {
    dispatch({ type: 'SET_ORDER_TYPE', payload: orderType });
  }, []);

  const setTableNumber = useCallback((tableNumber: number | undefined) => {
    dispatch({ type: 'SET_TABLE_NUMBER', payload: tableNumber });
  }, []);

  const setPaymentMethod = useCallback((paymentMethod: PaymentMethod) => {
    dispatch({ type: 'SET_PAYMENT_METHOD', payload: paymentMethod });
  }, []);

  const totals = useMemo<PosOrderTotals>(() => {
    const subtotalGross = state.items.reduce(
      (sum, item) =>
        sum +
        item.quantity *
          (item.unitGrossPrice +
            item.selectedModifiers.reduce((ms, m) => ms + m.priceAdjustment, 0)),
      0,
    );

    const vatPercent = state.orderType === 'EatIn' ? 21 : 6;
    // VAT is already included in gross price — extract it: vatAmount = gross * rate / (100 + rate)
    const vatAmount = (subtotalGross * vatPercent) / (100 + vatPercent);
    const totalGross = subtotalGross;

    return { subtotalGross, vatPercent, vatAmount, totalGross };
  }, [state.items, state.orderType]);

  const value: PosOrderContextValue = {
    state,
    totals,
    addItem,
    removeItem,
    updateQuantity,
    clearOrder,
    setOrderType,
    setTableNumber,
    setPaymentMethod,
    getModifierKey: modifierKey,
  };

  return <PosOrderContext.Provider value={value}>{children}</PosOrderContext.Provider>;
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

/**
 * Returns the current POS order context.
 * Throws if called outside a PosOrderProvider.
 */
// eslint-disable-next-line react-refresh/only-export-components -- HMR boundary rule; hook co-located with the context it consumes
export function useOrderState(): PosOrderContextValue {
  const ctx = useContext(PosOrderContext);
  if (ctx === null) {
    throw new Error('useOrderState must be used within a PosOrderProvider.');
  }
  return ctx;
}
