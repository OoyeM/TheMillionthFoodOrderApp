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
 */
export function getOrderHubConnection(): HubConnection {
  if (connection !== null) {
    return connection;
  }

  connection = new HubConnectionBuilder()
    .withUrl('/api/hubs/orders')
    .withAutomaticReconnect([0, 2000, 10000, 30000])
    .configureLogging(
      import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning,
    )
    .build();

  return connection;
}

/**
 * Resets the singleton connection. Used by tests and HMR cleanup.
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
