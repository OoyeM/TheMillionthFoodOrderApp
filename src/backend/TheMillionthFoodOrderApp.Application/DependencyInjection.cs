using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Application.Brands;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Application.MenuCategories;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Application.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IBrandSettingsService, BrandSettingsService>();
        services.AddScoped<IMenuCategoryService, MenuCategoryService>();
        services.AddScoped<IPlatformAdminService, PlatformAdminService>();
        services.AddScoped<IBrandStaffService, BrandStaffService>();
        services.AddScoped<IModifierGroupService, ModifierGroupService>();
        services.AddScoped<IOpeningHoursService, OpeningHoursService>();
        services.AddScoped<IOrderLifecycleService, OrderLifecycleService>();
        services.AddScoped<ITaxConfigurationService, TaxConfigurationService>();

        return services;
    }
}
