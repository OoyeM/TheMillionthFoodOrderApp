using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Identity;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;
using TheMillionthFoodOrderApp.Infrastructure.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        // Audit interceptor — shared across both DbContexts
        services.AddSingleton<AuditSaveChangesInterceptor>();

        // Multi-tenancy — scoped so it lives for one HTTP request
        services.AddScoped<BrandContextAccessor>();
        services.AddScoped<IBrandContextAccessor>(sp => sp.GetRequiredService<BrandContextAccessor>());

        // Brand context factory — scoped, uses the request's BrandContextAccessor
        services.AddScoped<BrandDbContextFactory>();

        // Repositories
        services.AddScoped<IBrandRepository, BrandRepository>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IShopRepository, ShopRepository>();

        // Seeders (scoped — they depend on scoped DbContext)
        services.AddScoped<PlatformDbSeeder>();
        services.AddScoped<BrandDbSeeder>();

        return services;
    }
}
