using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for <see cref="BrandDbContext"/> used exclusively by the
/// <c>dotnet ef migrations</c> CLI tool.
/// Uses a hardcoded local connection string — never used at runtime.
///
/// Usage:
/// <code>
///   dotnet ef migrations add InitialCreate \
///     --context BrandDbContext \
///     --project ../TheMillionthFoodOrderApp.Infrastructure \
///     --startup-project ../TheMillionthFoodOrderApp.Api \
///     --output-dir Persistence/Migrations/Brand
/// </code>
/// </summary>
public sealed class BrandDbContextDesignTimeFactory : IDesignTimeDbContextFactory<BrandDbContext>
{
    // Hardcoded for local development tooling only — SQL Server running via Aspire on default port
    private const string DesignTimeConnectionString =
        "Server=localhost,1433;Database=brand_design;User Id=sa;Password=Your_password123;TrustServerCertificate=True;";

    public BrandDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(DesignTimeConnectionString)
            .Options;

        return new BrandDbContext(options);
    }
}
