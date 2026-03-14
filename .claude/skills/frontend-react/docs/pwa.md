# PWA — Progressive Web App

## Three Apps, One Codebase

The frontend serves three distinct experiences from the same React codebase:

| App | URL Pattern | Target | Offline? |
|-----|------------|--------|----------|
| Customer Storefront | `/{brand-slug}/` | Phones, desktops | Nice-to-have |
| In-Store POS | `/{brand-slug}/pos/` | Tablets (touch) | **Required** |
| CMS Admin | `/{brand-slug}/admin/` | Desktops | No |

## Offline Requirements

### In-Store POS (Critical)
The POS must work when internet drops — a restaurant can't stop taking orders.

**Offline-capable features:**
- Browse menu / product catalog (cached on sync)
- Add items to order, apply modifiers
- Calculate totals (including VAT: 6% takeaway / 21% eat-in)
- Queue orders for sync when connection returns

**Not offline:**
- Payment processing (needs online)
- Real-time inventory updates

### Strategy: Cache-First for Catalog

```
Service Worker Strategy:
├── Product catalog → CacheFirst (refresh in background)
├── Images/assets → CacheFirst (long TTL)
├── API mutations → NetworkFirst with offline queue
└── Admin/CMS → NetworkOnly (no offline needed)
```

## vite-plugin-pwa Config

```ts
VitePWA({
  registerType: 'autoUpdate',
  includeAssets: ['favicon.ico', 'robots.txt'],
  manifest: {
    name: 'Frietjes?',  // dynamic per brand
    short_name: 'Frietjes?',
    theme_color: '#FF6B00',  // brand color
    display: 'standalone',
    start_url: '/',
  },
  workbox: {
    globPatterns: ['**/*.{js,css,html,ico,png,svg,woff2}'],
    runtimeCaching: [
      {
        urlPattern: /^\/api\/catalog\//,
        handler: 'CacheFirst',
        options: {
          cacheName: 'catalog-cache',
          expiration: { maxAgeSeconds: 3600 },
          backgroundSync: { name: 'catalog-sync' },
        },
      },
      {
        urlPattern: /^\/api\/orders/,
        handler: 'NetworkFirst',
        options: {
          cacheName: 'orders-cache',
          networkTimeoutSeconds: 3,
          plugins: [/* background sync plugin for offline queue */],
        },
      },
    ],
  },
})
```

## Installability

- Each app variant gets its own manifest (brand-specific name, colors, icons)
- POS devices: IT installs as PWA on tablet home screen
- Customer: prompted to install after second visit

## Background Sync

For orders created offline:
1. Order saved to IndexedDB
2. Service worker registers a `sync` event
3. When connection returns, queued orders POST to API
4. UI shows sync status indicator (pending/synced/failed)

## Testing Offline

- Chrome DevTools → Application → Service Workers → Offline checkbox
- Playwright: `context.setOffline(true)` in E2E tests
- Vitest: mock `navigator.onLine` for unit tests
