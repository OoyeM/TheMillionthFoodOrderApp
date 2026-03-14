using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Platform DB — in-memory for local development; replace with SQL Server for production
        services.AddDbContext<PlatformDbContext>(options =>
            options.UseInMemoryDatabase("PlatformDb"));

        services.AddScoped<IBrandRepository, BrandRepository>();

        return services;
    }
}
