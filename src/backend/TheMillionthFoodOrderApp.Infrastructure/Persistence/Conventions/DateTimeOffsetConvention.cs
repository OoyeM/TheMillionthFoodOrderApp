using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Conventions;

/// <summary>
/// Ensures all DateTimeOffset properties are stored as datetimeoffset in SQL Server.
/// SQL Server's datetimeoffset type preserves UTC offset correctly.
/// This convention acts as a safety net — the column type is set explicitly so it is
/// visible in migrations and does not depend on provider inference.
/// </summary>
public sealed class DateTimeOffsetConvention : IModelFinalizingConvention
{
    public void ProcessModelFinalizing(
        IConventionModelBuilder modelBuilder,
        IConventionContext<IConventionModelBuilder> context)
    {
        foreach (var entityType in modelBuilder.Metadata.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(DateTimeOffset) ||
                    property.ClrType == typeof(DateTimeOffset?))
                {
                    // RelationalPropertyBuilderExtensions.HasColumnType is available via
                    // the Microsoft.EntityFrameworkCore namespace (relational extension).
                    property.Builder.HasColumnType("datetimeoffset", fromDataAnnotation: false);
                }
            }
        }
    }
}
