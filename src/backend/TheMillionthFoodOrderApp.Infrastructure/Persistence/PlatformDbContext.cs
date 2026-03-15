using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Infrastructure.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Identity;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Conventions;
namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Shared platform database — stores cross-brand registry data such as brands,
/// platform admins, and global configuration.
/// All tables live in the "platform" schema to make the intent explicit.
/// </summary>
public sealed class PlatformDbContext(
    DbContextOptions<PlatformDbContext> options) : DbContext(options)
{
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<PlatformUser> PlatformUsers => Set<PlatformUser>();
    public DbSet<BrandUserRole> BrandUserRoles => Set<BrandUserRole>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Place all platform tables in a dedicated schema
        modelBuilder.HasDefaultSchema("platform");

        modelBuilder.ApplyConfiguration(new BrandConfiguration());
        modelBuilder.ApplyConfiguration(new PlatformUserConfiguration());
        modelBuilder.ApplyConfiguration(new BrandUserRoleConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.Conventions.Add(_ => new DateTimeOffsetConvention());
    }
}
