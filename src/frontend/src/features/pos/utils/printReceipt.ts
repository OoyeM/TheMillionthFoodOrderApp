import type { OrderResponse } from '@api/orders';
import { printHtmlDocument } from './printDocument';
import { formatTimeSlot } from '../../../utils/timeSlot';

/**
 * Localised, pre-resolved labels for the printed customer receipt. Kept caller-supplied
 * so the receipt utility stays i18n-agnostic and easy to unit test — the caller resolves
 * translations (including the VAT-rate interpolation and the payment-method display name).
 */
export interface ReceiptLabels {
  /** Heading printed at the top, e.g. "Receipt" / "Kasticket". */
  heading: string;
  /** Label preceding the shop's VAT number, e.g. "VAT no." / "Btw-nr.". */
  vatNumber: string;
  /** Resolved order-type text, e.g. "Eat-in" / "Afhalen". */
  orderType: string;
  /** Label preceding the table number, e.g. "Table". */
  table: string;
  /** Label preceding the time slot, e.g. "Time slot". */
  timeSlot: string;
  /** Label preceding the customer name, e.g. "Customer". */
  customer: string;
  /** Label preceding the timestamp, e.g. "Date". */
  placedAt: string;
  /** Label for the net (VAT-exclusive) total, e.g. "Total excl. VAT". */
  subtotalNet: string;
  /** Label for the VAT amount, with the rate already interpolated, e.g. "VAT 21%". */
  vat: string;
  /** Label for the gross (VAT-inclusive) total, e.g. "Total incl. VAT". */
  total: string;
  /** Label preceding the payment method, e.g. "Payment". */
  paymentMethod: string;
  /** Resolved payment-method display text, e.g. "Cash" / "Bancontact". */
  paymentMethodValue: string;
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

function formatCurrency(amount: number): string {
  return new Intl.NumberFormat('nl-BE', { style: 'currency', currency: 'EUR' }).format(amount);
}

/**
 * Builds a self-contained HTML document for a customer receipt, styled for a narrow
 * (~72mm) thermal receipt printer (US-FP-052). Pure function — the returned markup is
 * what gets written to the print iframe.
 *
 * Unlike the kitchen order ticket (US-FP-028), a receipt is a financial/legal record:
 * it carries the seller legal block (shop name, address, VAT number), per-line prices,
 * a Belgian VAT breakdown (net / VAT / gross), the payment method, and the date.
 */
export function buildReceiptHtml(order: OrderResponse, labels: ReceiptLabels): string {
  const placed = new Date(order.createdAt);
  const placedText = Number.isNaN(placed.getTime())
    ? order.createdAt
    : placed.toLocaleString('nl-BE', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
      });

  // Seller legal block — each line only renders when the server supplied it.
  const sellerRows: string[] = [];
  if (order.shopName != null && order.shopName.length > 0) {
    sellerRows.push(`<div class="seller-name">${escapeHtml(order.shopName)}</div>`);
  }
  if (order.shopAddressLine != null && order.shopAddressLine.length > 0) {
    sellerRows.push(`<div class="seller">${escapeHtml(order.shopAddressLine)}</div>`);
  }
  if (order.shopVatNumber != null && order.shopVatNumber.length > 0) {
    sellerRows.push(
      `<div class="seller">${escapeHtml(labels.vatNumber)}: ${escapeHtml(order.shopVatNumber)}</div>`,
    );
  }

  const metaRows: string[] = [`<div class="meta">${escapeHtml(labels.orderType)}</div>`];
  if (order.tableNumber != null) {
    metaRows.push(
      `<div class="meta">${escapeHtml(labels.table)}: ${escapeHtml(String(order.tableNumber))}</div>`,
    );
  }
  if (order.timeSlotStart != null && order.timeSlotEnd != null) {
    metaRows.push(
      `<div class="meta">${escapeHtml(labels.timeSlot)}: ${escapeHtml(formatTimeSlot(order.timeSlotStart, order.timeSlotEnd))}</div>`,
    );
  }
  if (order.customerName != null && order.customerName.length > 0) {
    metaRows.push(
      `<div class="meta">${escapeHtml(labels.customer)}: ${escapeHtml(order.customerName)}</div>`,
    );
  }
  metaRows.push(`<div class="meta">${escapeHtml(labels.placedAt)}: ${escapeHtml(placedText)}</div>`);

  const itemRows = order.items
    .map((item) => {
      const modifiers = item.selectedModifiers
        .map((m) => {
          const adj =
            m.priceAdjustment !== 0 ? ` (${formatCurrency(m.priceAdjustment)})` : '';
          return `<div class="mod">+ ${escapeHtml(m.modifierName)}${adj}</div>`;
        })
        .join('');
      return `<li class="item">
          <div class="item-line">
            <span class="qty">${String(item.quantity)}×</span>
            <span class="name">${escapeHtml(item.productName)}</span>
            <span class="line-total">${formatCurrency(item.lineTotal)}</span>
          </div>
          ${modifiers}
        </li>`;
    })
    .join('');

  return `<!doctype html>
<html>
<head>
<meta charset="utf-8" />
<title>${escapeHtml(labels.heading)} ${escapeHtml(order.orderNumber)}</title>
<style>
  * { margin: 0; padding: 0; box-sizing: border-box; }
  body { font-family: 'Courier New', monospace; width: 72mm; padding: 4mm; color: #000; }
  .seller-block { text-align: center; margin-bottom: 2mm; }
  .seller-name { font-size: 13px; font-weight: 700; }
  .seller { font-size: 11px; }
  .heading { text-align: center; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; }
  .number { text-align: center; font-size: 22px; font-weight: 700; margin: 2mm 0; }
  .meta { font-size: 11px; }
  hr { border: none; border-top: 1px dashed #000; margin: 3mm 0; }
  ul { list-style: none; }
  .item { margin-bottom: 2mm; }
  .item-line { display: flex; gap: 2mm; font-size: 13px; font-weight: 700; }
  .qty { min-width: 7mm; }
  .name { flex: 1; }
  .line-total { white-space: nowrap; }
  .mod { font-size: 11px; padding-left: 9mm; font-weight: 400; }
  .totals { font-size: 12px; }
  .totals .row { display: flex; justify-content: space-between; }
  .totals .grand { font-size: 14px; font-weight: 700; margin-top: 1mm; }
  .payment { font-size: 12px; margin-top: 2mm; display: flex; justify-content: space-between; }
  @page { margin: 0; }
</style>
</head>
<body>
  <div class="seller-block">${sellerRows.join('\n  ')}</div>
  <hr />
  <div class="heading">${escapeHtml(labels.heading)}</div>
  <div class="number">#${escapeHtml(order.orderNumber)}</div>
  ${metaRows.join('\n  ')}
  <hr />
  <ul>${itemRows}</ul>
  <hr />
  <div class="totals">
    <div class="row"><span>${escapeHtml(labels.subtotalNet)}</span><span>${formatCurrency(order.totalNet)}</span></div>
    <div class="row"><span>${escapeHtml(labels.vat)}</span><span>${formatCurrency(order.totalVatAmount)}</span></div>
    <div class="row grand"><span>${escapeHtml(labels.total)}</span><span>${formatCurrency(order.totalGross)}</span></div>
  </div>
  <div class="payment"><span>${escapeHtml(labels.paymentMethod)}</span><span>${escapeHtml(labels.paymentMethodValue)}</span></div>
</body>
</html>`;
}

/**
 * Prints a customer receipt via the shared hidden-iframe print mechanism. No-ops outside
 * the browser. Counter staff trigger this from the POS order-confirmation screen, and may
 * call it repeatedly to reprint (US-FP-052).
 */
export function printReceipt(order: OrderResponse, labels: ReceiptLabels): void {
  printHtmlDocument(buildReceiptHtml(order, labels));
}
