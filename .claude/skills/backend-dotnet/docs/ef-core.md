# EF Core

## Purpose
Data access layer. Azure SQL with database-per-brand for multi-tenant isolation.

## Database Strategy

- **Azure SQL** — single server instance, multiple databases
- **Platform database** — shared across all brands (brand registry, user accounts, platform config)
- **Brand databases** — one per brand, created dynamically at onboarding (catalog, orders, pricing, shop config)
- Relational model fits the domain: products ↔ modifiers (many-to-many), orders ↔ line items, VAT rules, approval workflows

## Multi-Tenant Resolution

- `BrandDbContextFactory` resolves the correct brand database connection string based on request context (brand slug from URL/header)
- Platform DB context is always available for cross-brand operations
- Brand DB deletion = full data isolation for GDPR compliance

## Patterns
<!-- Add patterns as they emerge during development -->

## Gotchas

- Always use `DateTimeOffset` instead of `DateTime` for all temporal columns
<!-- Add gotchas discovered during development -->
