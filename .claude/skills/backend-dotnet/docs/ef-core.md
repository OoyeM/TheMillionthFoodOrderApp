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

### Brand-Scoped Repository (per-method unit-of-work)

Brand entities (e.g., Shop) live in `BrandDbContext`, which is **not** DI-registered. Repositories inject `BrandDbContextFactory` and create a context per method call:

```csharp
public sealed class ShopRepository(BrandDbContextFactory dbContextFactory) : IShopRepository
{
    public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var db = dbContextFactory.CreateDbContext();
        return await db.Shops.FindAsync([id], ct);
    }

    // Mutations: load-mutate-save in a single context to keep change tracking intact
    public async Task<Shop?> UpdateAsync(Guid id, Action<Shop> mutate, CancellationToken ct = default)
    {
        await using var db = dbContextFactory.CreateDbContext();
        var shop = await db.Shops.FindAsync([id], ct);
        if (shop is null) return null;
        mutate(shop);
        await db.SaveChangesAsync(ct);
        return shop;
    }
}
```

Key rules:
- Each public method owns its full unit-of-work (`await using` context)
- `AddAsync` saves immediately — no separate `SaveChangesAsync` call needed
- `UpdateAsync` takes an `Action<T>` delegate to mutate the tracked entity within one context
- Do **not** expose `SaveChangesAsync` on brand-scoped repositories (it would be a no-op trap)
- `AuditSaveChangesInterceptor` is wired in `BrandDbContextFactory.CreateDbContext()` for safety

## Gotchas

- Always use `DateTimeOffset` instead of `DateTime` for all temporal columns
- **PlatformDbContext is pooled** (via Aspire's `AddSqlServerDbContext`) — never add interceptors or modify options in `OnConfiguring`. Use the `configureDbContextOptions` callback in `Program.cs` instead.
- **BrandDbContext is not in DI** — it's multi-tenant. Always resolve via `BrandDbContextFactory`, never inject `BrandDbContext` directly into constructors.
