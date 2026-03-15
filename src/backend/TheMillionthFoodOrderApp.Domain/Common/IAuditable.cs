namespace TheMillionthFoodOrderApp.Domain.Common;

/// <summary>
/// Marks an entity as auditable — EF Core interceptor will auto-populate these fields.
/// </summary>
public interface IAuditable
{
    DateTimeOffset CreatedAt { get; }
    DateTimeOffset UpdatedAt { get; }
}
