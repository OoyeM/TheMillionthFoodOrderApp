# Test Coverage Report — TheMillionthFoodOrderApp

_Generated: 2026-04-22_

## Summary

| Layer | Test Type | Files | Tests (≈) | State |
|---|---|---|---|---|
| Backend — Domain | xUnit unit | 8 | **89** | Partial |
| Backend — API / Application | xUnit integration | 17 | **140** | Mostly covered |
| Backend — BFF | — | 0 | 0 | **Untested** |
| Frontend — components / pages | Vitest smoke | 1 (`App.test.tsx`) | **3** | **Minimal** |
| Frontend — E2E | Playwright | 0 (no `e2e/` dir) | 0 | **Untested** |

Config exists for Vitest and Playwright, and CI wiring is in place (`pnpm test`, `pnpm test:e2e`), but the frontend has essentially no test suite.

---

## Backend — What IS tested

### Domain unit tests (`TheMillionthFoodOrderApp.Tests.Unit`)
Pure domain-logic coverage on the aggregates that have invariants worth protecting:

- **Products** — `Product`, `Money`, product sort-order (20 + 9 + 14 tests)
- **MenuCategories** — `MenuCategory` invariants, translations, sort order (14 tests)
- **ModifierGroups** — `ModifierGroup` + `Modifier` (14 tests)
- **TaxConfiguration** — `TaxCalculator`, `TaxConfiguration`, `VatRate` (5 + 8 + 5 tests)

### Integration tests (`TheMillionthFoodOrderApp.Tests.Integration`)
Real HTTP against `IntegrationTestWebAppFactory` with a real `BrandDbContext`. Mostly full CRUD + multi-tenant isolation:

| Area | CRUD | Isolation | Notes |
|---|---|---|---|
| Products | ✅ 16 | ✅ 2 | |
| MenuCategories | ✅ 21 | — | + category-product ordering (11) |
| ModifierGroups | ✅ 12 | ✅ 1 | + product-modifier link (6) |
| Shops (opening hours, status) | ✅ 13 + 4 | — | |
| OrderLifecycle | ✅ 10 | — | |
| TaxConfiguration | ✅ 11 | — | |
| BrandStaff | ✅ 10 | — | |
| PlatformAdmins | ✅ 7 | — | |
| BrandSettings | — | ✅ 4 | No CRUD tests |
| Multitenancy middleware | ✅ 4 | — | `BrandContextMiddleware` |
| SignalR `OrderHub` | ✅ 3 | — | Connection/auth only |
| `BrandDatabaseProvisioner` | ✅ 5 | — | DB-per-brand provisioning |

---

## Backend — What is NOT tested

### Domain aggregates with no unit tests
- **`Brand`** (`Brand.cs`, activate/deactivate, staff-auth config) — no tests
- **`Shop`** (`Shop.cs`, `Address`, `OpeningHoursTimeBlock`) — no domain-level tests
- **`BrandSettings`** (`BrandColors`, `BrandTypography`, `PresetFonts`) — no tests
- **`OrderLifecycleConfig`** (`OrderStatus`, `OrderStatusTransition` transitions) — no tests
- **Identity** (`PlatformUser`, `BrandUserRole`) — no tests

### API endpoints with no integration tests (10 endpoint groups, ~12 routes)
- **Brands** — Create, Update, Activate, Deactivate, Get, List, ConfigureStaffAuth (7 endpoints, 0 tests)
- **BrandSettings** — Get, Update, GetTheme, UpdateTheming, UploadLogo (5 endpoints, 0 tests — only isolation coverage)
- **Shops** — Create, Update, Get, List, Activate, Deactivate (6 endpoints, 0 tests — only status + opening hours + lifecycle covered)
- **ComboProducts** — Create, Update (2 endpoints, 0 tests)
- **Orders** — `SimulateOrderStatusChangeEndpoint` (0 tests — SignalR publishes via this path)

### BFF project — **zero tests**
`TheMillionthFoodOrderApp.Bff` has no test project at all:
- OIDC login / logout flow
- Cookie session + `session/keepalive`
- `MockAuthHandler` (dev default persona flow)
- `ClaimsEnrichmentService`
- YARP proxy + bearer-token forwarding

### Infrastructure with no direct tests (exercised only transitively)
- Repositories for Brand, BrandSettings, Shop, MenuCategory, ModifierGroup, Product, OrderLifecycleConfig, TaxConfiguration, PlatformUser (covered indirectly via integration CRUD where those routes are tested — otherwise not at all)
- `LocalFileStorageService` (logo upload) — no tests
- `BrandConnectionStringHelper`, `BrandDatabaseHealthCheck` — no tests
- `BrandContextAccessor` / `BrandContextValidator` — only tested via middleware test
- `OrderStatusChangedHandler`, `SignalROrderNotificationService` — only hub-connection test, no publish flow test

---

## Frontend — What IS tested

Only `src/App.test.tsx`:
- `Home` (storefront) renders "Welkom" heading
- `PosDashboard` renders
- `AdminDashboard` renders

That's it. Three render-smoke tests.

---

## Frontend — What is NOT tested

Practically everything. Vitest and Playwright are installed and scripted, but no tests exist beyond the three above.

- **23 of 24 admin pages** — BrandList/Create/Edit, BrandTheming, ShopList/Create/Edit, ShopOpeningHours, ShopOrderLifecycle, ProductList/Create/Edit, ComboProductCreate/Edit, ModifierGroupList/Create/Edit, MenuCategoryList/Create/Edit, PlatformAdminList, StaffList, TaxConfiguration — 0 tests
- **Storefront** — only `Home` render smoke; `LanguageSelector`, `ThemeProvider`, `ShopStatusBadge` untested
- **All 14 API client modules** (`auth.ts`, `brands.ts`, `products.ts`, `shops.ts`, `menuCategories.ts`, `modifierGroups.ts`, `openingHours.ts`, `orderLifecycle.ts`, `brandSettings.ts`, `brandStaff.ts`, `platformAdmins.ts`, `taxConfiguration.ts`, `client.ts`, `signalr.ts`) — 0 tests
- **Auth module** — `AuthContext`, `MockAuthProvider`, `BffAuthProvider`, `AuthProviderSwitch`, `RequireAuth`, `useSessionKeepalive` — 0 tests
- **Core components** — `AppShell` (brand + lang resolution), `AppVariantLayout`, `ErrorBoundary`, `SuspenseWrapper` — 0 tests
- **SignalR hooks** — `useSignalR`, `useOrderUpdates` — 0 tests
- **i18n** — locale files and config — 0 tests
- **Playwright E2E** — configured but `e2e/` directory doesn't exist

---

## Biggest coverage gaps (priority order)

1. **Whole frontend.** 3 smoke tests against 82 TS/TSX source files. Any API contract drift or routing change is silent.
2. **BFF.** Auth and session are the perimeter — currently no automated verification that login/logout, cookie handling, or the YARP bearer-forwarding work.
3. **Brand CRUD + Shop CRUD API endpoints.** Core CMS flows (create brand, create shop, activate/deactivate) have no HTTP-level tests.
4. **Combo products.** Entire feature (create + update) ships untested.
5. **BrandSettings CRUD + logo upload.** Only isolation is covered; update flows and `LocalFileStorageService` are not.
6. **Order status publish path.** `SimulateOrderStatusChangeEndpoint` → `OrderStatusChangedHandler` → SignalR is tested only for hub connectivity, not for end-to-end event delivery.
7. **Domain aggregates without unit tests** — `Brand`, `Shop`, `OrderLifecycleConfig`, `BrandSettings`. These have real invariants (activate/deactivate, opening-hours overlap, lifecycle transitions) that deserve pure unit coverage.
