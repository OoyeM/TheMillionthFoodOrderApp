using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Application.Brands;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IBrandService, BrandService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IShopService, ShopService>();
        services.AddScoped<IBrandSettingsService, BrandSettingsService>();

        return services;
    }
}
