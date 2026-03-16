namespace TheMillionthFoodOrderApp.Domain.MenuCategories;

public interface IMenuCategoryRepository
{
    Task<MenuCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MenuCategory>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(MenuCategory category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a dictionary mapping MenuCategoryId → product count.
    /// Only categories that have at least one product assigned are included;
    /// missing entries should be treated as zero.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the category with <paramref name="id"/> (WITHOUT translations),
    /// applies <paramref name="mutate"/>, and saves — all within a single tracked context.
    /// Clears and re-adds translations (use for full updates). Returns the mutated category, or null if not found.
    /// </summary>
    Task<MenuCategory?> UpdateAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the category with <paramref name="id"/> (without touching translations),
    /// applies <paramref name="mutate"/>, and saves. Returns the mutated category, or null if not found.
    /// Use this for mutations that do not modify the translation collection (e.g. reorder).
    /// </summary>
    Task<MenuCategory?> UpdateScalarAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
