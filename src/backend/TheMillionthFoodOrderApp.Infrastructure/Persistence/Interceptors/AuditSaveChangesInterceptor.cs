using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Automatically sets CreatedAt on insert and UpdatedAt on every save for entities
/// that implement <see cref="IAuditable"/>.
/// The Brand entity sets these values itself in its domain methods, but this interceptor
/// acts as a safety net for any entity added via EF directly without going through domain methods.
/// </summary>
public sealed class AuditSaveChangesInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        SetAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void SetAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State == EntityState.Added)
            {
                // Only set CreatedAt if it hasn't been set already (domain method may have set it)
                if (entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue is DateTimeOffset createdAt
                    && createdAt == default)
                    entry.Property(nameof(IAuditable.CreatedAt)).CurrentValue = now;

                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Property(nameof(IAuditable.UpdatedAt)).CurrentValue = now;

                // Never overwrite CreatedAt on update
                entry.Property(nameof(IAuditable.CreatedAt)).IsModified = false;
            }
        }
    }
}
