/**
 * Tests for src/api/orders.ts — CreateOrderRequest and in-store request shape.
 *
 * Verifies that:
 * - ordersApi.create sends customerFirstName, customerLastName, and languageCode
 * - ordersApi.createInStore sends customerFirstName and customerLastName (not customerName)
 * - OrderResponse still carries customerName (server-computed)
 */
import { describe, it, expect } from 'vitest';
import { http, HttpResponse } from 'msw';
import { server } from '../../test/msw/server';
import { ordersApi } from '../orders';
import type { CreateOrderRequest, CreateInStoreOrderRequest } from '../orders';

// ---------------------------------------------------------------------------
// Fixture helpers
// ---------------------------------------------------------------------------

function makeOrderResponse(overrides: Record<string, unknown> = {}) {
  return {
    id: 'order-1',
    orderNumber: 'ORD-001',
    shopId: 'shop-1',
    brandSlug: 'frietjes',
    orderType: 'Pickup',
    statusName: 'New',
    customerName: 'Jane Doe',
    customerFirstName: 'Jane',
    customerLastName: 'Doe',
    languageCode: 'nl',
    items: [],
    vatRatePercent: 6,
    subtotalGross: 3.5,
    totalVatAmount: 0.2,
    totalNet: 3.3,
    totalGross: 3.5,
    createdAt: '2024-06-01T10:00:00Z',
    paymentMethod: 'CashAtPickup',
    ...overrides,
  };
}

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('ordersApi', () => {
  describe('create (online order)', () => {
    it('posts customerFirstName, customerLastName, and languageCode to the order endpoint', async () => {
      let capturedBody: Record<string, unknown> | null = null;

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(makeOrderResponse(), { status: 201 });
        }),
      );

      const payload: CreateOrderRequest = {
        orderType: 'Pickup',
        customerFirstName: 'Jane',
        customerLastName: 'Doe',
        customerEmail: 'jane@example.com',
        customerPhone: '+32470000001',
        items: [{ productId: 'prod-1', quantity: 1, selectedModifierIds: [] }],
        paymentMethod: 'CashAtPickup',
        languageCode: 'nl',
      };

      const result = await ordersApi.create('frietjes', 'shop-1', payload);

      // Verify outgoing payload
      expect(capturedBody).not.toBeNull();
      expect(capturedBody!['customerFirstName']).toBe('Jane');
      expect(capturedBody!['customerLastName']).toBe('Doe');
      expect(capturedBody!['languageCode']).toBe('nl');
      // customerName must NOT be in the request body (backend derives it)
      expect('customerName' in capturedBody!).toBe(false);

      // Verify that the response customerName is preserved (display compat)
      expect(result.customerName).toBe('Jane Doe');
      expect(result.customerFirstName).toBe('Jane');
      expect(result.customerLastName).toBe('Doe');
      expect(result.languageCode).toBe('nl');
    });

    it('accepts fr as a valid languageCode', async () => {
      let capturedLang: unknown = null;

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders', async ({ request }) => {
          const body = await request.json() as Record<string, unknown>;
          capturedLang = body['languageCode'];
          return HttpResponse.json(makeOrderResponse({ languageCode: 'fr' }), { status: 201 });
        }),
      );

      await ordersApi.create('frietjes', 'shop-1', {
        orderType: 'Pickup',
        customerFirstName: 'Marie',
        customerLastName: 'Dupont',
        customerEmail: 'marie@example.be',
        customerPhone: '+32470000002',
        items: [{ productId: 'prod-1', quantity: 1, selectedModifierIds: [] }],
        paymentMethod: 'CashAtPickup',
        languageCode: 'fr',
      });

      expect(capturedLang).toBe('fr');
    });
  });

  describe('createInStore (POS order)', () => {
    it('posts customerFirstName and customerLastName (not customerName)', async () => {
      let capturedBody: Record<string, unknown> | null = null;

      server.use(
        http.post('/api/brands/:slug/shops/:shopId/orders/in-store', async ({ request }) => {
          capturedBody = await request.json() as Record<string, unknown>;
          return HttpResponse.json(
            {
              ...makeOrderResponse(),
              customerName: 'Staff Member',
              customerFirstName: 'Staff',
              customerLastName: 'Member',
            },
            { status: 201 },
          );
        }),
      );

      const payload: CreateInStoreOrderRequest = {
        orderType: 'Pickup',
        paymentMethod: 'CashAtPickup',
        customerFirstName: 'Staff',
        customerLastName: 'Member',
        items: [{ productId: 'prod-1', quantity: 1, selectedModifierIds: [] }],
      };

      await ordersApi.createInStore('frietjes', 'shop-1', payload);

      expect(capturedBody).not.toBeNull();
      expect(capturedBody!['customerFirstName']).toBe('Staff');
      expect(capturedBody!['customerLastName']).toBe('Member');
      // Old field must not be present in the request body
      expect('customerName' in capturedBody!).toBe(false);
    });
  });
});
