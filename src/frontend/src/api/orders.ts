import { apiClient } from './client';

// ---------------------------------------------------------------------------
// Request types
// ---------------------------------------------------------------------------

export type OrderType = 'Pickup' | 'EatIn' | 'Delivery';

export interface OrderItemRequest {
  productId: string;
  quantity: number;
  selectedModifierIds: string[];
}

export type PaymentMethod = 'CashAtPickup' | 'CreditCard' | 'Bancontact';

export interface CreateOrderRequest {
  orderType: OrderType;
  customerName?: string | null;
  customerEmail?: string | null;
  customerPhone?: string | null;
  items: OrderItemRequest[];
  paymentMethod: PaymentMethod;
}

// ---------------------------------------------------------------------------
// Response types
// ---------------------------------------------------------------------------

export interface OrderModifierResponse {
  modifierId: string;
  modifierName: string;
  priceAdjustment: number;
}

export interface OrderItemResponse {
  productId: string;
  productName: string;
  quantity: number;
  unitGrossPrice: number;
  unitNetPrice: number;
  unitVatAmount: number;
  lineTotal: number;
  selectedModifiers: OrderModifierResponse[];
}

export interface OrderResponse {
  id: string;
  orderNumber: string;
  shopId: string;
  brandSlug: string;
  orderType: OrderType;
  statusName: string;
  customerName: string | null;
  /** Optional email for digital receipt (US-FP-017). */
  customerEmail?: string | null;
  /** Optional phone number (US-FP-017). */
  customerPhone?: string | null;
  items: OrderItemResponse[];
  vatRatePercent: number;
  subtotalGross: number;
  totalVatAmount: number;
  totalNet: number;
  totalGross: number;
  createdAt: string;
  paymentMethod: string;
  // Forward-compatibility — server only sends timeSlot once that story ships.
  timeSlot?: string | null;
  /** Present on in-store EatIn orders. */
  tableNumber?: number;
  /** Staff member who created the order (set by the server from the auth token). */
  createdByStaffId?: string;
  /**
   * Seller legal block for receipts (US-FP-052) — the shop's name, VAT number, and a
   * single-line address. Present on order-create and order-tracking responses; absent on
   * the status-advance response consumed by the kitchen display.
   */
  shopName?: string | null;
  shopVatNumber?: string | null;
  shopAddressLine?: string | null;
}

export interface ListActiveOrdersResponse {
  orders: OrderResponse[];
}

/**
 * Request body for creating an in-store order (POS, staff only).
 * Route: POST /brands/{brandSlug}/shops/{shopId}/orders/in-store
 */
export interface CreateInStoreOrderRequest {
  orderType: OrderType;
  paymentMethod: PaymentMethod;
  customerName?: string;
  /** Required when orderType === 'EatIn'. */
  tableNumber?: number;
  items: OrderItemRequest[];
}

// ---------------------------------------------------------------------------
// Order tracking response types (US-FP-063)
// ---------------------------------------------------------------------------

export interface OrderStatusResponse {
  id: string;
  name: string;
  systemKey: string | null;
  sortOrder: number;
  isEnabled: boolean;
  isTerminal: boolean;
  colorHex: string | null;
}

export interface OrderStatusTransitionResponse {
  id: string;
  fromStatusId: string;
  toStatusId: string;
}

export interface OrderLifecycleResponse {
  shopId: string;
  statuses: OrderStatusResponse[];
  transitions: OrderStatusTransitionResponse[];
}

export interface OrderTrackingResponse {
  order: OrderResponse;
  lifecycle: OrderLifecycleResponse;
}

// ---------------------------------------------------------------------------
// API module
// ---------------------------------------------------------------------------

/**
 * API functions for orders (customer storefront + POS).
 * Routes are brand + shop scoped: /brands/{brandSlug}/shops/{shopId}/orders/...
 */
export const ordersApi = {
  create: (
    brandSlug: string,
    shopId: string,
    data: CreateOrderRequest,
  ): Promise<OrderResponse> =>
    apiClient
      .post<OrderResponse>(`/brands/${brandSlug}/shops/${shopId}/orders`, data)
      .then((r) => r.data),

  getById: (
    brandSlug: string,
    shopId: string,
    orderId: string,
  ): Promise<OrderResponse> =>
    apiClient
      .get<OrderTrackingResponse>(`/brands/${brandSlug}/shops/${shopId}/orders/${orderId}`)
      .then((r) => r.data.order),

  /** Fetch full tracking response (order + lifecycle) for the tracking page. */
  getTracking: (
    brandSlug: string,
    shopId: string,
    orderId: string,
  ): Promise<OrderTrackingResponse> =>
    apiClient
      .get<OrderTrackingResponse>(`/brands/${brandSlug}/shops/${shopId}/orders/${orderId}`)
      .then((r) => r.data),

  /** Look up an order by its human-readable order number. */
  getByNumber: (
    brandSlug: string,
    shopId: string,
    orderNumber: string,
  ): Promise<OrderTrackingResponse> =>
    apiClient
      .get<OrderTrackingResponse>(
        `/brands/${brandSlug}/shops/${shopId}/orders/number/${orderNumber}`,
      )
      .then((r) => r.data),

  /** List active (non-terminal) orders for a shop — backs the kitchen display (US-FP-027). */
  listActive: (brandSlug: string, shopId: string): Promise<OrderResponse[]> =>
    apiClient
      .get<ListActiveOrdersResponse>(`/brands/${brandSlug}/shops/${shopId}/orders/active`)
      .then((r) => r.data.orders),

  /**
   * Advance an order to the next status in the shop's lifecycle (US-FP-023).
   * Only transitions configured for the shop are accepted; the server pushes the
   * change to kitchen displays and the customer's tracking page via SignalR.
   * Route: POST /brands/{brandSlug}/shops/{shopId}/orders/{orderId}/status
   */
  advanceStatus: (
    brandSlug: string,
    shopId: string,
    orderId: string,
    toStatusId: string,
  ): Promise<OrderResponse> =>
    apiClient
      .post<OrderResponse>(
        `/brands/${brandSlug}/shops/${shopId}/orders/${orderId}/status`,
        { toStatusId },
      )
      .then((r) => r.data),

  /**
   * Create an in-store order via the POS interface (staff-only).
   * Route: POST /brands/{brandSlug}/shops/{shopId}/orders/in-store
   */
  createInStore: (
    brandSlug: string,
    shopId: string,
    data: CreateInStoreOrderRequest,
  ): Promise<OrderResponse> =>
    apiClient
      .post<OrderResponse>(
        `/brands/${brandSlug}/shops/${shopId}/orders/in-store`,
        data,
      )
      .then((r) => r.data),
};
