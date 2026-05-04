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

export interface CreateOrderRequest {
  orderType: OrderType;
  customerName?: string | null;
  items: OrderItemRequest[];
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
  items: OrderItemResponse[];
  vatRatePercent: number;
  subtotalGross: number;
  totalVatAmount: number;
  totalNet: number;
  totalGross: number;
  createdAt: string;
}

// ---------------------------------------------------------------------------
// API module
// ---------------------------------------------------------------------------

/**
 * API functions for orders (customer storefront).
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
      .get<OrderResponse>(`/brands/${brandSlug}/shops/${shopId}/orders/${orderId}`)
      .then((r) => r.data),
};
