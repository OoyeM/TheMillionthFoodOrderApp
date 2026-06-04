import {
  createContext,
  useContext,
  useReducer,
  useEffect,
  useCallback,
  type ReactNode,
} from 'react';

// ---------------------------------------------------------------------------
// Types
// ---------------------------------------------------------------------------

export interface CartModifier {
  modifierId: string;
  modifierName: string;
  priceAdjustment: number;
}

export interface CartItem {
  productId: string;
  productName: string;
  quantity: number;
  unitGrossPrice: number;
  selectedModifiers: CartModifier[];
}

export interface CartState {
  brandSlug: string;
  shopId: string;
  items: CartItem[];
}

// ---------------------------------------------------------------------------
// Actions
// ---------------------------------------------------------------------------

type CartAction =
  | { type: 'ADD_ITEM'; payload: CartItem }
  | { type: 'REMOVE_ITEM'; payload: { productId: string; modifierKey: string } }
  | { type: 'UPDATE_QUANTITY'; payload: { productId: string; modifierKey: string; quantity: number } }
  | { type: 'CLEAR_CART' }
  | { type: 'LOAD_CART'; payload: CartState };

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Creates a stable key for a cart item based on productId + selected modifiers.
 * This allows the same product with different modifier selections to be separate
 * line items.
 */
function modifierKey(selectedModifiers: CartModifier[]): string {
  return selectedModifiers
    .map((m) => m.modifierId)
    .sort()
    .join(',');
}

function storageKey(brandSlug: string, shopId: string): string {
  return `cart:${brandSlug}:${shopId}`;
}

function loadFromStorage(brandSlug: string, shopId: string): CartState | null {
  try {
    const raw = localStorage.getItem(storageKey(brandSlug, shopId));
    if (!raw) return null;
    const parsed = JSON.parse(raw) as CartState;
    // Validate that the stored state matches the current shop
    if (parsed.brandSlug !== brandSlug || parsed.shopId !== shopId) return null;
    return parsed;
  } catch {
    return null;
  }
}

function saveToStorage(state: CartState): void {
  try {
    localStorage.setItem(storageKey(state.brandSlug, state.shopId), JSON.stringify(state));
  } catch {
    // Storage quota exceeded — degrade gracefully
  }
}

// ---------------------------------------------------------------------------
// Reducer
// ---------------------------------------------------------------------------

function cartReducer(state: CartState, action: CartAction): CartState {
  switch (action.type) {
    case 'LOAD_CART':
      return action.payload;

    case 'ADD_ITEM': {
      const newItemKey = modifierKey(action.payload.selectedModifiers);
      const existingIndex = state.items.findIndex(
        (item) =>
          item.productId === action.payload.productId &&
          modifierKey(item.selectedModifiers) === newItemKey,
      );

      if (existingIndex >= 0) {
        // Increment quantity of existing line item
        const updatedItems = state.items.map((item, idx) =>
          idx === existingIndex
            ? { ...item, quantity: item.quantity + action.payload.quantity }
            : item,
        );
        return { ...state, items: updatedItems };
      }

      return { ...state, items: [...state.items, action.payload] };
    }

    case 'REMOVE_ITEM': {
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

    case 'UPDATE_QUANTITY': {
      if (action.payload.quantity <= 0) {
        // Remove item when quantity drops to 0
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
    }

    case 'CLEAR_CART':
      return { ...state, items: [] };

    default:
      return state;
  }
}

// ---------------------------------------------------------------------------
// Context
// ---------------------------------------------------------------------------

interface CartContextValue {
  state: CartState;
  addItem: (item: CartItem) => void;
  removeItem: (productId: string, modifiers: CartModifier[]) => void;
  updateQuantity: (productId: string, modifiers: CartModifier[], quantity: number) => void;
  clearCart: () => void;
  cartTotal: number;
  cartItemCount: number;
  getModifierKey: (modifiers: CartModifier[]) => string;
}

const CartContext = createContext<CartContextValue | null>(null);

// ---------------------------------------------------------------------------
// Provider
// ---------------------------------------------------------------------------

interface CartProviderProps {
  brandSlug: string;
  shopId: string;
  children: ReactNode;
}

export function CartProvider({ brandSlug, shopId, children }: CartProviderProps) {
  const initialState: CartState = {
    brandSlug,
    shopId,
    items: [],
  };

  const [state, dispatch] = useReducer(cartReducer, initialState, () => {
    // Eagerly load from localStorage on mount
    const stored = loadFromStorage(brandSlug, shopId);
    return stored ?? initialState;
  });

  // When shopId changes (user navigates to a different shop), load that shop's cart
  useEffect(() => {
    const stored = loadFromStorage(brandSlug, shopId);
    if (stored) {
      dispatch({ type: 'LOAD_CART', payload: stored });
    } else {
      dispatch({
        type: 'LOAD_CART',
        payload: { brandSlug, shopId, items: [] },
      });
    }
  }, [brandSlug, shopId]);

  // Persist to localStorage on every change
  useEffect(() => {
    // Only persist if this state belongs to the current shop
    if (state.brandSlug === brandSlug && state.shopId === shopId) {
      saveToStorage(state);
    }
  }, [state, brandSlug, shopId]);

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

  const clearCart = useCallback(() => {
    dispatch({ type: 'CLEAR_CART' });
  }, []);

  const cartTotal = state.items.reduce(
    (sum, item) =>
      sum +
      item.quantity *
        (item.unitGrossPrice +
          item.selectedModifiers.reduce((mSum, m) => mSum + m.priceAdjustment, 0)),
    0,
  );

  const cartItemCount = state.items.reduce((sum, item) => sum + item.quantity, 0);

  const value: CartContextValue = {
    state,
    addItem,
    removeItem,
    updateQuantity,
    clearCart,
    cartTotal,
    cartItemCount,
    getModifierKey: modifierKey,
  };

  return <CartContext.Provider value={value}>{children}</CartContext.Provider>;
}

// ---------------------------------------------------------------------------
// Hook
// ---------------------------------------------------------------------------

// eslint-disable-next-line react-refresh/only-export-components -- hook co-located with its provider; HMR boundary only, not a correctness concern
export function useCart(): CartContextValue {
  const ctx = useContext(CartContext);
  if (ctx === null) {
    throw new Error('useCart must be used within a CartProvider.');
  }
  return ctx;
}
