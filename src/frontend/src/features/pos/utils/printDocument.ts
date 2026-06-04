/**
 * Prints a self-contained HTML document via a transient hidden iframe so the
 * surrounding app UI is never affected by print styles. No-ops outside the
 * browser (tests/SSR).
 *
 * Each call isolates one document, which lets callers (e.g. the kitchen display)
 * queue several prints without them bleeding into one another. Shared by the
 * kitchen order ticket (US-FP-028) and the customer receipt (US-FP-052).
 */
export function printHtmlDocument(html: string): void {
  if (typeof document === 'undefined') return;

  const iframe = document.createElement('iframe');
  iframe.setAttribute('aria-hidden', 'true');
  iframe.style.position = 'fixed';
  iframe.style.right = '0';
  iframe.style.bottom = '0';
  iframe.style.width = '0';
  iframe.style.height = '0';
  iframe.style.border = '0';

  iframe.addEventListener('load', () => {
    const frameWindow = iframe.contentWindow;
    if (frameWindow == null) {
      iframe.remove();
      return;
    }
    try {
      frameWindow.focus();
      frameWindow.print();
    } finally {
      // Give the print dialog time to grab the document before tearing it down.
      window.setTimeout(() => iframe.remove(), 1000);
    }
  });

  document.body.appendChild(iframe);

  const doc = iframe.contentWindow?.document ?? iframe.contentDocument;
  if (doc == null) {
    iframe.remove();
    return;
  }
  doc.open();
  doc.write(html);
  doc.close();
}
