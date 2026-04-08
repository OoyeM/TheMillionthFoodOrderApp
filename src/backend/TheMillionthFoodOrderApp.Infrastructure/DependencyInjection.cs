using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.BrandSettings;
using TheMillionthFoodOrderApp.Infrastructure.FileStorage;
using TheMillionthFoodOrderApp.Infrastructure.Identity;
using TheMillionthFoodOrderApp.Infrastructure.MenuCategories;
using TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;
using TheMillionthFoodOrderApp.Infrastructure.Notifications;
using TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;
using TheMillionthFoodOrderApp.Infrastructure.Products;
using TheMillionthFoodOrderApp.Infrastructure.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Audit interceptor — shared across both DbContexts
        services.AddSingleton<AuditSaveChangesInterceptor>();

        // IMemoryCache for brand validation TTL (required by BrandContextValidator)
        services.AddMemoryCache();

        // Multi-tenancy — scoped so it lives for one HTTP request
        services.AddScoped<BrandContextAccessor>();
        services.AddScoped<IBrandContextAccessor>(sp => sp.GetRequiredService<BrandContextAccessor>());
        services.AddScoped<IBrandContextValidator, BrandContextValidator>();

        // Brand context factory — scoped, uses the request's BrandContextAccessor
        services.AddScoped<BrandDbContextFactory>();

        // BrandDbContext registered as scoped via factory so it is available for injection
        // throughout the request lifetime (after BrandContextMiddleware has set the slug).
        // Returns null-object context when no brand slug is set — this happens at startup
        // when FastEndpoints instantiates endpoints to build the route map.
        services.AddScoped<Persistence.BrandDbContext>(sp =>
        {
            var factory = sp.GetRequiredService<BrandDbContextFactory>();
            return factory.CreateDbContext();
        });

        // Repositories
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IBrandSettingsRepository, BrandSettingsRepository>();
        services.AddScoped<IMenuCategoryRepository, MenuCategoryRepository>();
        services.AddScoped<IModifierGroupRepository, ModifierGroupRepository>();
        services.AddScoped<IOrderLifecycleConfigRepository, OrderLifecycleConfigRepository>();

        // Seeders (scoped — they depend on scoped DbContext)
        services.AddScoped<PlatformDbSeeder>();
        services.AddScoped<BrandDbSeeder>();

        // File storage — LocalFileStorageService is the dev/default implementation.
        // The caller (Api/Program.cs) configures LocalFileStorageOptions with the correct
        // uploads path. In production, replace this registration with an Azure Blob Storage
        // implementation without changing Application or API layers.
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        // Real-time notifications — SignalR implementation
        services.AddScoped<IOrderNotificationService, SignalROrderNotificationService>();

        return services;
    }
}
