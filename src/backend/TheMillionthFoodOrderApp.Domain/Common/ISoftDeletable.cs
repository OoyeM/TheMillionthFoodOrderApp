namespace TheMillionthFoodOrderApp.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable — hidden from queries but retained in the database.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTimeOffset? DeletedAt { get; }
}
