# EF Core

## Purpose
Data access layer. Azure SQL with database-per-brand for multi-tenant isolation.

## Database Strategy

- **Azure SQL** — single server instance, multiple databases
- **Platform database** — shared across all brands (brand registry, user accounts, platform config)
- **Brand databases** — one per brand, created dynamically at onboarding (catalog, orders, pricing, shop config)
- Relational model fits the domain: products <-> modifiers (many-to-many), orders <-> line items, VAT rules, approval workflows

## Multi-Tenant Resolution

- `BrandContextMiddleware` extracts slug from route `{brandSlug}` or `X-Brand-Slug` header, validates against platform DB (cached 30s), stores in `BrandContextAccessor`
- `BrandDbContextFactory` resolves the correct brand database connection string by swapping `Initial Catalog` to `brand_{slug}` via `SqlConnectionStringBuilder`
- `BrandDbContext` is registered as **scoped** in DI via factory delegate — inject it directly into brand-scoped repositories
- Platform DB context is always available for cross-brand operations
- Brand DB deletion = full data isolation for GDPR compliance

## Patterns

### Entity Configuration
One `IEntityTypeConfiguration<T>` per entity, applied in `OnModelCreating`:

```csharp
public sealed class ThingConfiguration : IEntityTypeConfiguration<Thing>
{
    public void Configure(EntityTypeBuilder<Thing> builder)
    {
        builder.ToTable("Things");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Ignore(t => t.DomainEvents); // transient, never persisted
    }
}
```

### Repository Pattern
Domain defines interface, Infrastructure implements with DbContext injection:

```csharp
// Domain layer
public interface IThingRepository
{
    Task<Thing?> GetAsync(CancellationToken ct);
    Task AddAsync(Thing thing, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}

// Infrastructure layer — inject BrandDbContext for brand-scoped, PlatformDbContext for platform
public sealed class ThingRepository(BrandDbContext dbContext) : IThingRepository { ... }
```

### DateTimeOffset Convention
The `DateTimeOffsetConvention` in both DbContexts ensures all `DateTimeOffset` properties map to `datetimeoffset(7)` in SQL Server. No manual column type annotations needed.

## Gotchas

- **Always use `DateTimeOffset`** instead of `DateTime` for all temporal columns.
- **PlatformDbContext is pooled** (via Aspire's `AddSqlServerDbContext`) — never add interceptors or modify options in `OnConfiguring`. Use the `configureDbContextOptions` callback in `Program.cs` instead.
- **BrandDbContext is scoped via factory** — registered in DI as `services.AddScoped<BrandDbContext>(sp => factory.CreateDbContext())`. If no brand slug is set (non-brand-scoped request), the factory throws `InvalidOperationException`. This means injecting `BrandDbContext` into a platform-only service will fail fast at resolve time — this is the desired behavior.
- **BrandDbSeeder uses BrandDbContextFactory directly** — not the DI-registered BrandDbContext, because seeding happens at startup before the middleware runs. Set `brandContextAccessor.BrandSlug` manually before calling seeder.
- **AuditSaveChangesInterceptor** must be added to both contexts: PlatformDbContext via Aspire's `configureDbContextOptions`, BrandDbContext via `BrandDbContextFactory.CreateDbContext()`.
- **Migrations are separate**: Platform migrations in `Persistence/Migrations/Platform/`, Brand migrations in `Persistence/Migrations/Brand/`. Brand migrations are applied by `BrandDatabaseProvisioner` when a new brand DB is created.
