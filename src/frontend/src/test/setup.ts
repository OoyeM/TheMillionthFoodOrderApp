// fallow-ignore-file unused-file
//
// Vitest setup file — loaded by vite.config.ts via setupFiles option.
// Fallow doesn't statically resolve string-config references.

import '@testing-library/jest-dom/vitest';
import { afterAll, afterEach, beforeAll } from 'vitest';
import { server } from './msw/server';

beforeAll(() => { server.listen({ onUnhandledRequest: 'error' }); });
afterEach(() => { server.resetHandlers(); });
afterAll(() => { server.close(); });
