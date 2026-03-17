using Microsoft.Data.SqlClient;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Centralizes brand database connection string derivation.
/// All callers that need a brand- or master-scoped connection string must use
/// these methods — never duplicate the <see cref="SqlConnectionStringBuilder"/> logic.
/// </summary>
public static class BrandConnectionStringHelper
{
    /// <summary>
    /// Derives a brand-specific connection string by swapping the database name
    /// in the platform connection string to <c>brand_{slug}</c>.
    /// </summary>
    /// <param name="platformConnectionString">The platform connection string (any catalog).</param>
    /// <param name="brandSlug">The brand slug — must be pre-validated/sanitized by the caller.</param>
    /// <returns>A connection string pointing at <c>brand_{brandSlug}</c>.</returns>
    public static string DeriveBrandConnectionString(string platformConnectionString, string brandSlug)
    {
        var builder = new SqlConnectionStringBuilder(platformConnectionString)
        {
            InitialCatalog = $"brand_{brandSlug}"
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Derives a <c>master</c>-scoped connection string from the platform connection string.
    /// Required for DDL operations such as <c>CREATE DATABASE</c> and <c>sys.databases</c> queries.
    /// </summary>
    /// <param name="platformConnectionString">The platform connection string (any catalog).</param>
    /// <returns>A connection string pointing at the <c>master</c> database.</returns>
    public static string DeriveMasterConnectionString(string platformConnectionString)
    {
        var builder = new SqlConnectionStringBuilder(platformConnectionString)
        {
            InitialCatalog = "master"
        };

        return builder.ConnectionString;
    }
}
