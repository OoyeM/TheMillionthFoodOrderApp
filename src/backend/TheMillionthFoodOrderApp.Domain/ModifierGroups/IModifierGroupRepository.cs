namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

public interface IModifierGroupRepository
{
    Task<ModifierGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ModifierGroup>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ModifierGroup modifierGroup, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the modifier group with <paramref name="id"/> (including translations and modifiers
    /// with their translations), applies <paramref name="mutate"/>, and saves — all within a
    /// single transaction. Returns the mutated group, or null if not found.
    /// </summary>
    Task<ModifierGroup?> UpdateAsync(Guid id, Action<ModifierGroup> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all ProductModifierGroup join records for the given product,
    /// ordered by SortOrder ascending.
    /// </summary>
    Task<IReadOnlyList<ProductModifierGroup>> GetProductModifierGroupsAsync(Guid productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all ProductModifierGroup assignments for <paramref name="productId"/>
    /// with <paramref name="assignments"/> within a single transaction.
    /// </summary>
    Task SetProductModifierGroupsAsync(Guid productId, IEnumerable<(Guid modifierGroupId, int sortOrder)> assignments, CancellationToken cancellationToken = default);
}
