using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Brands;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Shared platform database — stores cross-brand registry data such as brands,
/// platform admins, and global configuration.
/// </summary>
public sealed class PlatformDbContext(DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfiguration(new BrandConfiguration());
    }
}
