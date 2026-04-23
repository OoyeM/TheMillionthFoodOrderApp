import { setupServer } from 'msw/node';
import { handlers } from './handlers';

/**
 * MSW server instance for Vitest (Node environment).
 * Started/reset/stopped via the global setup in src/test/setup.ts.
 */
export const server = setupServer(...handlers);
