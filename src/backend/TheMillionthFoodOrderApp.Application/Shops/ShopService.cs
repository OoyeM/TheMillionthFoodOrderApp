using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.Shops;

public sealed class ShopService(IShopRepository shopRepository) : IShopService
{
    public async Task<ShopResponse> CreateShopAsync(
        CreateShopRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await shopRepository.GetBySlugAsync(request.Slug, cancellationToken);
        if (existing is not null)
            throw new InvalidOperationException($"A shop with slug '{request.Slug}' already exists in this brand.");

        var address = MapToAddress(request.Address);
        var shop = Shop.Create(request.Name, request.Slug, address, request.ContactEmail, request.ContactPhone);

        await shopRepository.AddAsync(shop, cancellationToken);

        return MapToResponse(shop);
    }

    public async Task<ShopResponse> UpdateShopAsync(
        Guid id,
        UpdateShopRequest request,
        CancellationToken cancellationToken = default)
    {
        var address = MapToAddress(request.Address);

        var shop = await shopRepository.UpdateAsync(
            id,
            s => s.UpdateMetadata(request.Name, address, request.ContactEmail, request.ContactPhone),
            cancellationToken);

        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{id}' was not found.");

        return MapToResponse(shop);
    }

    public async Task DeactivateShopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.UpdateAsync(id, s => s.Deactivate(), cancellationToken);

        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{id}' was not found.");
    }

    public async Task ActivateShopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.UpdateAsync(id, s => s.Activate(), cancellationToken);

        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{id}' was not found.");
    }

    public async Task<ShopResponse> GetShopAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(id, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{id}' was not found.");

        return MapToResponse(shop);
    }

    public async Task<IReadOnlyList<ShopResponse>> GetShopsAsync(CancellationToken cancellationToken = default)
    {
        var shops = await shopRepository.GetAllAsync(cancellationToken);
        return shops.Select(MapToResponse).ToList().AsReadOnly();
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static Address MapToAddress(AddressRequest req) =>
        new(req.Street, req.Number, req.City, req.PostalCode, req.Country);

    private static ShopResponse MapToResponse(Shop shop) =>
        new(
            shop.Id,
            shop.Name,
            shop.Slug,
            new AddressResponse(
                shop.Address.Street,
                shop.Address.Number,
                shop.Address.City,
                shop.Address.PostalCode,
                shop.Address.Country),
            shop.ContactEmail,
            shop.ContactPhone,
            shop.IsActive,
            shop.CreatedAt,
            shop.UpdatedAt);
}
