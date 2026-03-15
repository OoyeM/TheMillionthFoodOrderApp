using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Conventions;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Per-brand database context. Each brand has its own isolated SQL Server database.
/// Entities specific to a brand (shops, products, orders, etc.) will be registered here
/// as the domain grows. Currently a shell — schema and migration history are established
/// so the provisioner can create and migrate brand databases on demand.
/// </summary>
public sealed class BrandDbContext(DbContextOptions<BrandDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All brand-specific tables live in the default "dbo" schema.
        // Add IEntityTypeConfiguration<T> registrations here as brand entities are introduced.
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new DateTimeOffsetConvention());
    }
}
