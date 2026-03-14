# 006 — US-FP-001: Brand Management (Full Stack)

**Date:** 2026-03-14

## What happened

Implemented the first user story end-to-end: Platform Admins can create, edit, activate, and deactivate brands. This is the first feature with real API endpoints and a working admin UI.

## Backend

### Domain layer
- `Brand` aggregate root with factory method (`Create`), `UpdateMetadata`, `Deactivate`, `Activate`
- Domain events: `BrandCreatedEvent`, `BrandDeactivatedEvent`
- `IBrandRepository` interface

### Application layer
- `BrandService` with full CRUD + activate/deactivate
- DTOs: `CreateBrandRequest`, `UpdateBrandRequest`, `BrandResponse`

### Infrastructure layer
- `PlatformDbContext` (EF Core) — shared platform database with `DbSet<Brand>`
- `BrandConfiguration` — unique index on slug, column constraints
- `BrandRepository` implementation
- Using EF Core InMemory for local dev (no SQL Server required)

### API layer (FastEndpoints)
| Endpoint | Verb | Route |
|----------|------|-------|
| CreateBrand | POST | `/api/brands` |
| UpdateBrand | PUT | `/api/brands/{id}` |
| GetBrand | GET | `/api/brands/{id}` |
| ListBrands | GET | `/api/brands` |
| DeactivateBrand | POST | `/api/brands/{id}/deactivate` |
| ActivateBrand | POST | `/api/brands/{id}/activate` |

All endpoints have FluentValidation validators (slug format, email format, required fields).

## Frontend

### API + hooks
- `src/api/brands.ts` — axios client for all brand endpoints
- `src/features/admin/hooks/useBrands.ts` — TanStack Query hooks with cache invalidation

### Pages
- **BrandList** — table with status badges, activate/deactivate toggle, create button
- **BrandCreate** — form with auto-slug from name, validation
- **BrandEdit** — pre-filled form, read-only slug, status display

### Routes
- `/:brandSlug/:lang/admin/` redirects to `/admin/brands`
- `/admin/brands` — list, `/admin/brands/new` — create, `/admin/brands/:brandId` — edit

## Key decisions

- **MassTransit replaced with Wolverine 5.19.1** — MassTransit went commercial; Wolverine is the OSS alternative by Jeremy Miller. Registered via `builder.Host.UseWolverine()` in API Program.cs. No consumers yet, just the bus infrastructure.
- **EF Core InMemory for local dev** — no database setup needed to run the app. Will switch to SQL Server for production.
- **Swagger as default launch page** — `launchUrl: "swagger"` in launchSettings.json for faster API exploration.
- **Vite proxy fixed** — was pointing to `localhost:5000`, corrected to `localhost:5102` (actual API port) and removed the path rewrite since API routes already include `/api` prefix.
- **Brand type simplified** — frontend `Brand` interface changed from `LocalizedString` name to flat `string` to match the API. Localized names will come in a future iteration.

## What's not yet wired

- Database provisioning on brand creation (records `databaseName` but doesn't create the actual DB)
- Auth/authorization — no platform admin role enforcement yet
- Brand deactivation doesn't cascade to shops (shop entity doesn't exist yet)
- No unit tests for this feature yet

## Lessons

1. **Parallel agent implementation works** — backend and frontend agents ran simultaneously with zero conflicts since they touch different directories.
2. **Version compatibility matters** — Wolverine 4.x targets .NET 9; needed 5.x for .NET 10. Always check latest package versions.
3. **Vite proxy config is easy to miss** — the scaffold had a placeholder port; real API port needs to be documented in CLAUDE.md for future sessions.
