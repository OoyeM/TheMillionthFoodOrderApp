using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Infrastructure.BrandSettings;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Conventions;
using TheMillionthFoodOrderApp.Infrastructure.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Per-brand database context. Each brand has its own isolated SQL Server database.
/// Brand-specific entities (settings, shops, products, orders, etc.) are registered here.
/// </summary>
public sealed class BrandDbContext(DbContextOptions<BrandDbContext> options) : DbContext(options)
{
    public DbSet<Domain.BrandSettings.BrandSettings> BrandSettings => Set<Domain.BrandSettings.BrandSettings>();
    public DbSet<Shop> Shops => Set<Shop>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All brand-specific tables live in the default "dbo" schema.
        modelBuilder.ApplyConfiguration(new BrandSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ShopConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new DateTimeOffsetConvention());
    }
}
