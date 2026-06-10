import { describe, it, expect } from 'vitest';
import { buildTicketHtml, type TicketLabels } from '../printTicket';
import { formatTimeSlot } from '../../../../utils/timeSlot';
import type { OrderResponse } from '@api/orders';

const labels: TicketLabels = {
  heading: 'Order ticket',
  orderType: 'Eat-in',
  table: 'Table',
  timeSlot: 'Time slot',
  placedAt: 'Placed',
  customer: 'Customer',
};

function makeOrder(overrides: Partial<OrderResponse> = {}): OrderResponse {
  return {
    id: 'order-1',
    orderNumber: '0042',
    shopId: 'shop-1',
    brandSlug: 'frietjes',
    orderType: 'EatIn',
    statusName: 'Placed',
    customerName: null,
    items: [
      {
        productId: 'p1',
        productName: 'Frietje Speciaal',
        quantity: 2,
        unitGrossPrice: 3.5,
        unitNetPrice: 3.3,
        unitVatAmount: 0.2,
        lineTotal: 7,
        selectedModifiers: [
          { modifierId: 'm1', modifierName: 'Extra mayo', priceAdjustment: 0 },
        ],
      },
    ],
    vatRatePercent: 21,
    subtotalGross: 7,
    totalVatAmount: 1.2,
    totalNet: 5.8,
    totalGross: 7,
    createdAt: '2026-06-04T10:15:00Z',
    paymentMethod: 'CashAtPickup',
    ...overrides,
  };
}

describe('buildTicketHtml', () => {
  it('includes the order number, items, quantities, and modifiers', () => {
    const html = buildTicketHtml(makeOrder(), labels);
    expect(html).toContain('#0042');
    expect(html).toContain('Frietje Speciaal');
    expect(html).toContain('2×');
    expect(html).toContain('+ Extra mayo');
  });

  it('includes the order type and a timestamp', () => {
    const html = buildTicketHtml(makeOrder(), labels);
    expect(html).toContain('Eat-in');
    expect(html).toContain('Placed:');
  });

  it('renders the table number only for orders that have one', () => {
    expect(buildTicketHtml(makeOrder({ tableNumber: 5 }), labels)).toContain('Table: 5');
    // Default order omits tableNumber entirely (Pickup-style).
    expect(buildTicketHtml(makeOrder(), labels)).not.toContain('Table:');
  });

  it('renders the time slot only when both timeSlotStart and timeSlotEnd are present', () => {
    // Exact local-time text depends on the host timezone, so assert against the same
    // formatter the ticket uses — this still catches swapped args or a wrong source field.
    const start = '2026-06-10T17:30:00Z';
    const end = '2026-06-10T17:40:00Z';
    const withSlot = buildTicketHtml(
      makeOrder({ timeSlotStart: start, timeSlotEnd: end }),
      labels,
    );
    expect(withSlot).toContain(`Time slot: ${formatTimeSlot(start, end)}`);

    // When both are null, the row must be absent.
    expect(buildTicketHtml(makeOrder(), labels)).not.toContain('Time slot:');
  });

  it('renders the customer name when present', () => {
    expect(buildTicketHtml(makeOrder({ customerName: 'Alice' }), labels)).toContain('Customer: Alice');
  });

  it('escapes HTML in user-controlled fields to prevent markup injection', () => {
    const html = buildTicketHtml(
      makeOrder({ customerName: '<script>alert(1)</script>' }),
      labels,
    );
    expect(html).not.toContain('<script>alert(1)</script>');
    expect(html).toContain('&lt;script&gt;');
  });
});
