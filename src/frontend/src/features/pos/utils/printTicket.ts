import type { OrderResponse } from '@api/orders';
import { printHtmlDocument } from './printDocument';

/**
 * Localised, pre-resolved labels for the printed ticket. Kept caller-supplied so
 * the print utility stays i18n-agnostic and easy to unit test.
 */
export interface TicketLabels {
  /** Heading printed at the top of the ticket, e.g. "Order ticket". */
  heading: string;
  /** Resolved order-type text, e.g. "Eat-in" / "Afhalen". */
  orderType: string;
  /** Label preceding the table number, e.g. "Table". */
  table: string;
  /** Label preceding the time slot, e.g. "Time slot". */
  timeSlot: string;
  /** Label preceding the timestamp, e.g. "Placed". */
  placedAt: string;
  /** Label preceding the customer name, e.g. "Customer". */
  customer: string;
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#39;');
}

/**
 * Builds a self-contained HTML document for an order ticket, styled for a
 * narrow (~72mm) thermal/kitchen printer. Pure function — returned markup is
 * what gets written to the print iframe (US-FP-028).
 *
 * Includes order number, items with modifiers, order type, table number
 * (eat-in only), time slot (when present), and the order timestamp.
 */
export function buildTicketHtml(order: OrderResponse, labels: TicketLabels): string {
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

  const metaRows: string[] = [`<div class="meta">${escapeHtml(labels.orderType)}</div>`];

  if (order.tableNumber != null) {
    metaRows.push(
      `<div class="meta">${escapeHtml(labels.table)}: ${escapeHtml(String(order.tableNumber))}</div>`,
    );
  }
  if (order.timeSlot != null && order.timeSlot.length > 0) {
    metaRows.push(
      `<div class="meta">${escapeHtml(labels.timeSlot)}: ${escapeHtml(order.timeSlot)}</div>`,
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
        .map((m) => `<div class="mod">+ ${escapeHtml(m.modifierName)}</div>`)
        .join('');
      return `<li class="item">
          <div class="item-line"><span class="qty">${String(item.quantity)}×</span><span class="name">${escapeHtml(item.productName)}</span></div>
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
  .heading { text-align: center; font-size: 12px; text-transform: uppercase; letter-spacing: 1px; }
  .number { text-align: center; font-size: 28px; font-weight: 700; margin: 2mm 0; }
  .meta { font-size: 12px; }
  hr { border: none; border-top: 1px dashed #000; margin: 3mm 0; }
  ul { list-style: none; }
  .item { margin-bottom: 2mm; }
  .item-line { display: flex; gap: 2mm; font-size: 14px; font-weight: 700; }
  .qty { min-width: 7mm; }
  .mod { font-size: 12px; padding-left: 9mm; font-weight: 400; }
  @page { margin: 0; }
</style>
</head>
<body>
  <div class="heading">${escapeHtml(labels.heading)}</div>
  <div class="number">#${escapeHtml(order.orderNumber)}</div>
  ${metaRows.join('\n  ')}
  <hr />
  <ul>${itemRows}</ul>
</body>
</html>`;
}

/**
 * Prints an order ticket via a transient hidden iframe so the surrounding app
 * UI is never affected by print styles. No-ops outside the browser (tests/SSR).
 *
 * Each call isolates one ticket, which lets the kitchen display queue several
 * auto-prints without them bleeding into one another.
 */
export function printTicket(order: OrderResponse, labels: TicketLabels): void {
  printHtmlDocument(buildTicketHtml(order, labels));
}
