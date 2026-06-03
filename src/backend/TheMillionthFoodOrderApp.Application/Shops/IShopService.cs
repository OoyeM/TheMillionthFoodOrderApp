namespace TheMillionthFoodOrderApp.Application.Shops;

public interface IShopService
{
    Task<ShopResponse> CreateShopAsync(CreateShopRequest request, CancellationToken cancellationToken = default);
    Task<ShopResponse> UpdateShopAsync(Guid id, UpdateShopRequest request, CancellationToken cancellationToken = default);
    Task DeactivateShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopResponse> GetShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopResponse>> GetShopsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns only active shops, enriched with real-time open/closed status.
    /// Intended for the public storefront — does not expose admin-only fields.
    /// </summary>
    Task<IReadOnlyList<StorefrontShopResponse>> GetActiveShopsAsync(CancellationToken cancellationToken = default);
}
