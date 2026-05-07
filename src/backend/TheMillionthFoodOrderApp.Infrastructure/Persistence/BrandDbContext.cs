using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;
using TheMillionthFoodOrderApp.Infrastructure.BrandSettings;
using TheMillionthFoodOrderApp.Infrastructure.MenuCategories;
using TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;
using TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;
using TheMillionthFoodOrderApp.Infrastructure.Orders;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Conventions;
using TheMillionthFoodOrderApp.Infrastructure.Products;
using TheMillionthFoodOrderApp.Infrastructure.Shops;
using TheMillionthFoodOrderApp.Infrastructure.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Per-brand database context. Each brand has its own isolated SQL Server database.
/// Brand-specific entities (settings, shops, products, orders, etc.) are registered here.
/// </summary>
public sealed class BrandDbContext(DbContextOptions<BrandDbContext> options) : DbContext(options)
{
    public DbSet<Domain.BrandSettings.BrandSettings> BrandSettings => Set<Domain.BrandSettings.BrandSettings>();
    public DbSet<Shop> Shops => Set<Shop>();
    public DbSet<OpeningHoursTimeBlock> OpeningHoursTimeBlocks => Set<OpeningHoursTimeBlock>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductTranslation> ProductTranslations => Set<ProductTranslation>();
    public DbSet<MenuCategory> MenuCategories => Set<MenuCategory>();
    public DbSet<MenuCategoryTranslation> MenuCategoryTranslations => Set<MenuCategoryTranslation>();
    public DbSet<ModifierGroup> ModifierGroups => Set<ModifierGroup>();
    public DbSet<ModifierGroupTranslation> ModifierGroupTranslations => Set<ModifierGroupTranslation>();
    public DbSet<Modifier> Modifiers => Set<Modifier>();
    public DbSet<ModifierTranslation> ModifierTranslations => Set<ModifierTranslation>();
    public DbSet<ProductModifierGroup> ProductModifierGroups => Set<ProductModifierGroup>();
    public DbSet<OrderLifecycleConfig> OrderLifecycleConfigs => Set<OrderLifecycleConfig>();
    public DbSet<OrderStatus> OrderStatuses => Set<OrderStatus>();
    public DbSet<OrderStatusTransition> OrderStatusTransitions => Set<OrderStatusTransition>();
    public DbSet<ComboItem> ComboItems => Set<ComboItem>();
    public DbSet<Domain.TaxConfiguration.TaxConfiguration> TaxConfigurations => Set<Domain.TaxConfiguration.TaxConfiguration>();
    public DbSet<VatRate> VatRates => Set<VatRate>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // All brand-specific tables live in the default "dbo" schema.
        modelBuilder.ApplyConfiguration(new BrandSettingsConfiguration());
        modelBuilder.ApplyConfiguration(new ShopConfiguration());
        modelBuilder.ApplyConfiguration(new OpeningHoursTimeBlockConfiguration());
        modelBuilder.ApplyConfiguration(new ProductConfiguration());
        modelBuilder.ApplyConfiguration(new ProductTranslationConfiguration());
        modelBuilder.ApplyConfiguration(new MenuCategoryConfiguration());
        modelBuilder.ApplyConfiguration(new MenuCategoryTranslationConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierGroupConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierGroupTranslationConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierConfiguration());
        modelBuilder.ApplyConfiguration(new ModifierTranslationConfiguration());
        modelBuilder.ApplyConfiguration(new ProductModifierGroupConfiguration());
        modelBuilder.ApplyConfiguration(new OrderLifecycleConfigConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusConfiguration());
        modelBuilder.ApplyConfiguration(new OrderStatusTransitionConfiguration());
        modelBuilder.ApplyConfiguration(new ComboItemConfiguration());
        modelBuilder.ApplyConfiguration(new TaxConfigurationConfiguration());
        modelBuilder.ApplyConfiguration(new VatRateConfiguration());
        modelBuilder.ApplyConfiguration(new OrderConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemConfiguration());

        // Global query filter: soft-deleted entities are excluded by default
        modelBuilder.Entity<Product>().HasQueryFilter(p => !p.IsDeleted);
        modelBuilder.Entity<MenuCategory>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<ModifierGroup>().HasQueryFilter(g => !g.IsDeleted);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new DateTimeOffsetConvention());
    }
}
