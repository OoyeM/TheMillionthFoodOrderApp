namespace TheMillionthFoodOrderApp.Application.Shops;

public interface IShopService
{
    Task<ShopResponse> CreateShopAsync(CreateShopRequest request, CancellationToken cancellationToken = default);
    Task<ShopResponse> UpdateShopAsync(Guid id, UpdateShopRequest request, CancellationToken cancellationToken = default);
    Task DeactivateShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task ActivateShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShopResponse> GetShopAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ShopResponse>> GetShopsAsync(CancellationToken cancellationToken = default);
}
