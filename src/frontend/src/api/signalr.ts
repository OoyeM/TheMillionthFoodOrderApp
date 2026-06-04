// Real-time SignalR client + hooks. Wired up by US-FP-068
// (Real-time order updates infrastructure).

import {
  HubConnectionBuilder,
  HubConnection,
  LogLevel,
} from '@microsoft/signalr';

let connection: HubConnection | null = null;

/**
 * Returns or creates the singleton SignalR connection to the order hub.
 * The connection auto-reconnects on drops with exponential backoff.
 *
 * Auth: cookies are sent automatically on the WebSocket handshake
 * (Vite proxies /api to BFF, which forwards the bearer token to the API).
 *
 * CSRF: the BFF rejects authenticated POSTs under /api without the `X-CSRF: 1`
 * header (see CsrfHeaderMiddleware). SignalR's negotiate is such a POST, so we
 * send the header here — matching the axios client (`src/api/client.ts`).
 * The header applies to the HTTP negotiate/long-polling/SSE requests; the
 * WebSocket upgrade itself is a GET and is not CSRF-checked.
 */
export function getOrderHubConnection(): HubConnection {
  if (connection !== null) {
    return connection;
  }

  connection = new HubConnectionBuilder()
    .withUrl('/api/hubs/orders', {
      headers: { 'X-CSRF': '1' },
    })
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
    )
    .build();

  return connection;
}

/**
 * Resets the singleton connection. Intended for tests and HMR cleanup.
 * @expected-unused — retained as a test/HMR reset utility; not yet imported
 */
export function resetOrderHubConnection(): void {
  connection = null;
}

// Clean up the connection on Vite HMR to prevent duplicate connections in dev.
if (import.meta.hot) {
  import.meta.hot.dispose(() => {
    void connection?.stop();
    connection = null;
  });
}
