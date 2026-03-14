# Vite Configuration

## Project Setup

- **pnpm** as package manager (workspace-ready)
- Vite 5+ with React plugin
- Path aliases via `vite.config.ts` and `tsconfig.json`

## Config Structure

```ts
// vite.config.ts
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import { VitePWA } from 'vite-plugin-pwa';
import tsconfigPaths from 'vite-tsconfig-paths';

export default defineConfig({
  plugins: [
    react(),
    tsconfigPaths(),
    VitePWA({ /* see pwa.md */ }),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'http://localhost:5000', // BFF
        changeOrigin: true,
      },
    },
  },
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          vendor: ['react', 'react-dom', 'react-router-dom'],
          query: ['@tanstack/react-query'],
        },
      },
    },
  },
});
```

## Path Aliases

```json
// tsconfig.json compilerOptions.paths
{
  "@/*": ["./src/*"],
  "@features/*": ["./src/features/*"],
  "@components/*": ["./src/components/*"],
  "@api/*": ["./src/api/*"],
  "@types/*": ["./src/types/*"]
}
```

## Environment Variables

- Prefix with `VITE_` for client-side access
- Use `.env.local` for secrets (gitignored)
- Type-safe env via `src/env.d.ts`:

```ts
/// <reference types="vite/client" />
interface ImportMetaEnv {
  readonly VITE_API_URL: string;
  readonly VITE_BRAND_SLUG: string;
}
```

## Dev Server

- HMR enabled by default
- Proxy `/api` to BFF backend (avoids CORS in dev)
- Port: 5173 (default Vite)

## Build Output

- `dist/` — production build
- Code splitting: route-level lazy loading + manual vendor chunks
- Source maps: enabled for staging, disabled for production
