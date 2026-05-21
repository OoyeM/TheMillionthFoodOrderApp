import { useReducer } from 'react';
import type { OrderType } from '@api/orders';

// ---------------------------------------------------------------------------
// State types
// ---------------------------------------------------------------------------

export interface PosTicketModifier {
  modifierId: string;
  modifierName: string;
  priceAdjustment: number;
}

export interface PosTicketItem {
  /** Unique key within the ticket — productId plus a modifier fingerprint for deduplication. */
  key: string;
  productId: string;
  productName: string;
  unitGrossPrice: number;
  quantity: number;
  selectedModifiers: PosTicketModifier[];
}

export interface PosOrderState {
  items: PosTicketItem[];
  orderType: OrderType;
  tableNumber: string;
  customerName: string;
}

// ---------------------------------------------------------------------------
// Action types
// ---------------------------------------------------------------------------

type AddItemAction = {
  type: 'ADD_ITEM';
  payload: Omit<PosTicketItem, 'key' | 'quantity'>;
};

type RemoveItemAction = {
  type: 'REMOVE_ITEM';
  payload: { key: string };
};

type UpdateQuantityAction = {
  type: 'UPDATE_QUANTITY';
  payload: { key: string; quantity: number };
};

type SetOrderTypeAction = {
  type: 'SET_ORDER_TYPE';
  payload: { orderType: OrderType };
};

type SetTableNumberAction = {
  type: 'SET_TABLE_NUMBER';
  payload: { tableNumber: string };
};

type SetCustomerNameAction = {
  type: 'SET_CUSTOMER_NAME';
  payload: { customerName: string };
};

type ResetAction = { type: 'RESET' };

export type PosOrderAction =
  | AddItemAction
  | RemoveItemAction
  | UpdateQuantityAction
  | SetOrderTypeAction
  | SetTableNumberAction
  | SetCustomerNameAction
  | ResetAction;

// ---------------------------------------------------------------------------
// Initial state
// ---------------------------------------------------------------------------

const initialState: PosOrderState = {
  items: [],
  orderType: 'Pickup',
  tableNumber: '',
  customerName: '',
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Generates a stable key for a ticket item based on the product ID and selected
 * modifier IDs (sorted). Two items with the same product and same modifiers are
 * treated as the same line and their quantities are merged.
 */
function buildItemKey(productId: string, modifierIds: string[]): string {
  const sortedIds = [...modifierIds].sort().join(',');
  return sortedIds ? `${productId}__${sortedIds}` : productId;
}

// ---------------------------------------------------------------------------
// Reducer
// ---------------------------------------------------------------------------

function posOrderReducer(state: PosOrderState, action: PosOrderAction): PosOrderState {
  switch (action.type) {
    case 'ADD_ITEM': {
      const { productId, productName, unitGrossPrice, selectedModifiers } = action.payload;
      const modifierIds = selectedModifiers.map((m) => m.modifierId);
      const key = buildItemKey(productId, modifierIds);

      const existingIndex = state.items.findIndex((i) => i.key === key);
      if (existingIndex >= 0) {
        // Merge into existing line
        const updated = state.items.map((item, idx) =>
          idx === existingIndex ? { ...item, quantity: item.quantity + 1 } : item,
        );
        return { ...state, items: updated };
      }

      const newItem: PosTicketItem = {
        key,
        productId,
        productName,
        unitGrossPrice,
        quantity: 1,
        selectedModifiers,
      };
      return { ...state, items: [...state.items, newItem] };
    }

    case 'REMOVE_ITEM': {
      return {
        ...state,
        items: state.items.filter((i) => i.key !== action.payload.key),
      };
    }

    case 'UPDATE_QUANTITY': {
      const { key, quantity } = action.payload;
      if (quantity <= 0) {
        return { ...state, items: state.items.filter((i) => i.key !== key) };
      }
      return {
        ...state,
        items: state.items.map((i) =>
          i.key === key ? { ...i, quantity: Math.min(quantity, 99) } : i,
        ),
      };
    }

    case 'SET_ORDER_TYPE': {
      return {
        ...state,
        orderType: action.payload.orderType,
        // Clear table number when switching away from eat-in
        tableNumber:
          action.payload.orderType !== 'EatIn' ? '' : state.tableNumber,
      };
    }

    case 'SET_TABLE_NUMBER': {
      return { ...state, tableNumber: action.payload.tableNumber };
    }

    case 'SET_CUSTOMER_NAME': {
      return { ...state, customerName: action.payload.customerName };
    }

    case 'RESET': {
      return { ...initialState };
    }

    default:
      return state;
  }
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

/**
 * In-memory POS order state managed with useReducer.
 * NOT persisted to localStorage — counter staff move fast and a stale ticket
 * on reload is worse than starting fresh. Contrast with storefront CartContext.
 */
export function usePosOrder() {
  const [state, dispatch] = useReducer(posOrderReducer, initialState);

  const subtotal = state.items.reduce(
    (sum, item) => sum + item.unitGrossPrice * item.quantity,
    0,
  );

  return { state, dispatch, subtotal };
}
