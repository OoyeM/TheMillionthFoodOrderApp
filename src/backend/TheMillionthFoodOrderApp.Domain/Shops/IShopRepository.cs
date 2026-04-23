namespace TheMillionthFoodOrderApp.Domain.Shops;

public interface IShopRepository
{
    Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Shop>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Shop shop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the shop with <paramref name="id"/>, applies <paramref name="mutate"/>,
    /// and saves — all within a single tracked context.
    /// Returns the mutated shop, or null if not found.
    /// </summary>
    Task<Shop?> UpdateAsync(Guid id, Action<Shop> mutate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all OpeningHoursTimeBlocks for the given shop within a transaction.
    /// Bypasses EF change tracker to avoid snapshot / cascade conflicts.
    /// Returns the updated shop, or null if not found.
    /// </summary>
    Task<Shop?> ReplaceOpeningHoursAsync(Guid shopId, Action<Shop> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
