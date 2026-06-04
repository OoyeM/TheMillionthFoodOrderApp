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
        var shop = Shop.Create(request.Name, request.Slug, address, request.ContactEmail, request.ContactPhone, request.VatNumber);

        await shopRepository.AddAsync(shop, cancellationToken);
        await shopRepository.SaveChangesAsync(cancellationToken);

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
            s =>
            {
                s.UpdateMetadata(request.Name, address, request.ContactEmail, request.ContactPhone, request.VatNumber);
                s.SetKitchenDisplayEnabled(request.KitchenDisplayEnabled);
                s.SetTicketPrinterEnabled(request.TicketPrinterEnabled);
                s.SetPushNotificationEnabled(request.PushNotificationEnabled);
                s.SetSoundAlertEnabled(request.SoundAlertEnabled);
                s.SetEatInSettings(request.EatIn.IsEnabled, request.EatIn.RequiresTableNumber);
                s.SetTimeSlotOrdering(MapTimeSlotOrdering(request.TimeSlotOrdering));
            },
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

    public async Task<IReadOnlyList<StorefrontShopResponse>> GetActiveShopsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var shops = await shopRepository.GetActiveAsync(cancellationToken);
        return shops.Select(s => MapToStorefrontResponse(s, now)).ToList().AsReadOnly();
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
            shop.KitchenDisplayEnabled,
            shop.TicketPrinterEnabled,
            shop.PushNotificationEnabled,
            shop.SoundAlertEnabled,
            new EatInSettingsDto(shop.EatIn.IsEnabled, shop.EatIn.RequiresTableNumber),
            new TimeSlotOrderingSettingsDto(
                shop.TimeSlotOrdering.IsEnabled,
                (int?)shop.TimeSlotOrdering.Interval,
                shop.TimeSlotOrdering.MaxOrdersPerInterval),
            shop.CreatedAt,
            shop.UpdatedAt,
            shop.VatNumber);

    private static StorefrontShopResponse MapToStorefrontResponse(Shop shop, DateTimeOffset now) =>
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
            shop.IsOpenAt(now),
            new EatInSettingsDto(shop.EatIn.IsEnabled, shop.EatIn.RequiresTableNumber));

    private static TimeSlotOrderingSettings MapTimeSlotOrdering(TimeSlotOrderingSettingsDto dto) =>
        dto.IsEnabled && dto.IntervalMinutes is int minutes && dto.MaxOrdersPerInterval is int max
            ? TimeSlotOrderingSettings.Enabled((TimeSlotInterval)minutes, max)
            : TimeSlotOrderingSettings.Disabled();
}
