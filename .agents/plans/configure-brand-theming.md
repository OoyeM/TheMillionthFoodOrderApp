# Implementation Plan: US-FP-029 — Configure Brand Theming

## Overview

Extend the existing `BrandSettings` aggregate with theming capabilities: brand colors (primary/secondary/accent), typography (font family selection from a preset list), logo upload, and a custom domain field (stored only, actual DNS routing deferred to US-FP-067). The storefront dynamically applies the brand theme via CSS custom properties fetched from a public endpoint. All shops under the brand automatically inherit the theme.

## Requirements (from Acceptance Criteria)

- Brand Admin can upload a logo and set primary/secondary/accent colors
- Brand Admin can select fonts from a preset list (custom upload deferred)
- Brand Admin can configure a custom domain field (DNS routing is US-FP-067)
- All shops under the brand inherit and operate within this theming
- Changes to theming are reflected on the storefront after save (no deploy needed)

## Key Design Decisions

1. **Extend BrandSettings, do not create a separate entity.** BrandSettings is already a singleton per brand DB. Theming is brand-level config, not a new aggregate. Adding properties to the existing entity avoids a JOIN and keeps the one-settings-row-per-brand invariant.

2. **Colors and Typography as EF-owned value objects.** `BrandColors` (Primary, Secondary, Accent) and `BrandTypography` (HeadingFontFamily, BodyFontFamily) are value objects owned by BrandSettings. This gives structured storage, validation at the domain level, and clean column naming.

3. **Preset font list, not custom upload.** The MVP uses a curated list of web-safe + Google Fonts families. This avoids file storage complexity for fonts, licensing issues, and performance concerns.

4. **Logo stored as a URL, uploaded via a separate endpoint.** A dedicated `POST /api/brands/{brandSlug}/settings/logo` endpoint accepts multipart form data and returns the URL. File storage is abstracted behind `IFileStorageService` (local filesystem in dev, Azure Blob Storage in prod).

5. **Public theme endpoint for the storefront.** `GET /api/brands/{brandSlug}/theme` returns a lightweight DTO with only the visual properties needed by the storefront. AllowAnonymous.

6. **CSS custom properties for runtime theming.** The storefront fetches the theme once on load (cached by TanStack Query) and injects CSS custom properties on the root element. No redeploy needed.

7. **All theming fields are nullable with sensible defaults.** A brand that has not configured theming gets a neutral default theme.

---

## Implementation Phases

### Phase 1: Domain Layer (3 new files, 1 modified)
- `BrandColors` value object (Primary, Secondary, Accent hex colors with validation)
- `BrandTypography` value object (HeadingFontFamily, BodyFontFamily validated against preset list)
- `PresetFonts` static constant (Inter, Roboto, Open Sans, Lato, Poppins, Montserrat, Nunito, Raleway, Source Sans 3, DM Sans, System Default)
- Extend `BrandSettings` with LogoUrl, CustomDomain, Colors, Typography + UpdateTheming/SetLogoUrl methods

### Phase 2: Infrastructure Layer (2 new files, 3 modified, 1 migration)
- EF config for owned types (BrandColors, BrandTypography as nullable owned entities)
- `IFileStorageService` abstraction + `LocalFileStorageService` (stores to wwwroot/uploads/)
- Register in DI, add `UseStaticFiles()` for serving uploaded logos
- EF migration: `AddBrandTheming` (additive, nullable columns only)

### Phase 3: Application Layer (2 modified files)
- Extend DTOs: BrandColorsDto, BrandTypographyDto, UpdateBrandThemingRequest, BrandThemeResponse
- Extend service: UpdateThemingAsync, UploadLogoAsync, GetThemeAsync

### Phase 4: API Endpoints (3 new endpoints)
- `PUT /api/brands/{brandSlug}/settings/theming` — update colors, typography, custom domain
- `POST /api/brands/{brandSlug}/settings/logo` — multipart logo upload
- `GET /api/brands/{brandSlug}/theme` — public, lightweight theme for storefront

### Phase 5: Frontend — Admin UI (4 new files, 2 modified)
- TypeScript types, API client, TanStack Query hooks
- BrandTheming admin page: logo upload, color pickers, font dropdowns, custom domain input, live preview
- Route + navigation link

### Phase 6: Frontend — Storefront Theme Application (2 new files, 1 modified)
- `useBrandTheme` hook (fetches theme with 5min staleTime)
- `ThemeProvider` component (injects CSS custom properties + loads Google Fonts)
- Integrate into storefront layout

### Phase 7: Testing and Seed Data
- Seed Frietjes? brand with sample theming
- Integration tests: theming CRUD, validation, logo upload, cross-brand isolation
- Verify existing tests still pass

---

## Default Theme Values

| Property | Default |
|----------|---------|
| Primary | #111827 (gray-900) |
| Secondary | #6b7280 (gray-500) |
| Accent | #2563eb (blue-600) |
| Heading Font | System Default |
| Body Font | System Default |
| Logo | null (show brand name text) |
| Custom Domain | null |

---

## Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Nullable owned entities in EF Core | MEDIUM | Configure all columns as nullable, test migration carefully |
| First multipart file upload in codebase | MEDIUM | Use FastEndpoints' built-in file binding, keep simple |
| Google Font loading performance | LOW | Use font-display: swap and preconnect hints |
| Theme flicker on storefront load | LOW | Acceptable for MVP; SSR optimization can follow |

---

## Dependencies

**Prerequisites (all ✅):** US-FP-001, US-FP-004, US-FP-070
**Unblocks:** US-FP-067 (custom domain routing), US-FP-055 (PWA)
