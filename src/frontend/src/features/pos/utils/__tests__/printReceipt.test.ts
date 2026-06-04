import { describe, it, expect } from 'vitest';
import { buildReceiptHtml, type ReceiptLabels } from '../printReceipt';
import type { OrderResponse } from '@api/orders';

const labels: ReceiptLabels = {
  heading: 'Receipt',
  vatNumber: 'VAT no.',
  orderType: 'Eat-in',
  table: 'Table',
  timeSlot: 'Time slot',
  customer: 'Customer',
  placedAt: 'Date',
  subtotalNet: 'Total excl. VAT',
  vat: 'VAT 21%',
  total: 'Total incl. VAT',
  paymentMethod: 'Payment',
  paymentMethodValue: 'Cash',
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
        unitNetPrice: 2.89,
        unitVatAmount: 0.61,
        lineTotal: 7,
        selectedModifiers: [
          { modifierId: 'm1', modifierName: 'Extra mayo', priceAdjustment: 0.5 },
        ],
      },
    ],
    vatRatePercent: 21,
    subtotalGross: 7,
    totalVatAmount: 1.21,
    totalNet: 5.79,
    totalGross: 7,
    createdAt: '2026-06-04T10:15:00Z',
    paymentMethod: 'CashAtPickup',
    shopName: 'Frietjes Gent',
    shopVatNumber: 'BE0123456789',
    shopAddressLine: 'Korenmarkt 1, 9000 Gent',
    ...overrides,
  };
}

describe('buildReceiptHtml', () => {
  it('renders the seller legal block: shop name, address, and VAT number', () => {
    const html = buildReceiptHtml(makeOrder(), labels);
    expect(html).toContain('Frietjes Gent');
    expect(html).toContain('Korenmarkt 1, 9000 Gent');
    expect(html).toContain('VAT no.: BE0123456789');
  });

  it('omits legal lines the server did not supply', () => {
    const html = buildReceiptHtml(
      makeOrder({ shopVatNumber: null, shopAddressLine: null }),
      labels,
    );
    expect(html).toContain('Frietjes Gent');
    expect(html).not.toContain('VAT no.:');
    expect(html).not.toContain('Korenmarkt');
  });

  it('includes the order number, items, quantities, line totals, and modifiers', () => {
    const html = buildReceiptHtml(makeOrder(), labels);
    expect(html).toContain('#0042');
    expect(html).toContain('Frietje Speciaal');
    expect(html).toContain('2×');
    expect(html).toContain('Extra mayo');
  });

  it('renders the Belgian VAT breakdown: net, VAT amount, and gross total', () => {
    const html = buildReceiptHtml(makeOrder(), labels);
    expect(html).toContain('Total excl. VAT');
    expect(html).toContain('VAT 21%');
    expect(html).toContain('Total incl. VAT');
    // Amounts are formatted as EUR currency (nl-BE) — assert the numeric portions appear.
    expect(html).toContain('5,79');
    expect(html).toContain('1,21');
    expect(html).toContain('7,00');
  });

  it('renders the payment method', () => {
    const html = buildReceiptHtml(makeOrder(), labels);
    expect(html).toContain('Payment');
    expect(html).toContain('Cash');
  });

  it('renders the table number only for orders that have one', () => {
    expect(buildReceiptHtml(makeOrder({ tableNumber: 5 }), labels)).toContain('Table: 5');
    expect(buildReceiptHtml(makeOrder(), labels)).not.toContain('Table:');
  });

  it('includes the order type and a timestamp', () => {
    const html = buildReceiptHtml(makeOrder(), labels);
    expect(html).toContain('Eat-in');
    expect(html).toContain('Date:');
  });

  it('escapes HTML in user-controlled fields to prevent markup injection', () => {
    const html = buildReceiptHtml(
      makeOrder({ customerName: '<script>alert(1)</script>', shopName: '<b>x</b>' }),
      labels,
    );
    expect(html).not.toContain('<script>alert(1)</script>');
    expect(html).toContain('&lt;script&gt;');
    expect(html).not.toContain('<b>x</b>');
  });
});
