using System.Globalization;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.Shops;

public sealed class OpeningHoursService(IShopRepository shopRepository) : IOpeningHoursService
{
    /// <inheritdoc/>
    public async Task<OpeningHoursResponse> SetOpeningHoursAsync(
        Guid shopId,
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var blocks = request.TimeBlocks
            .Select(b => OpeningHoursTimeBlock.Create(
                shopId,
                b.DayOfWeek,
                TimeOnly.Parse(b.OpenTime, CultureInfo.InvariantCulture),
                TimeOnly.Parse(b.CloseTime, CultureInfo.InvariantCulture)))
            .ToList();

        shop.SetOpeningHours(blocks);

        await shopRepository.SaveChangesAsync(cancellationToken);

        return MapToResponse(shop);
    }

    /// <inheritdoc/>
    public async Task<OpeningHoursResponse> GetOpeningHoursAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        return MapToResponse(shop);
    }

    /// <inheritdoc/>
    public async Task<ShopStatusResponse> GetShopStatusAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var now = DateTimeOffset.UtcNow;
        var isOpen = shop.IsOpenAt(now);
        var nextOpeningTime = isOpen ? null : shop.GetNextOpeningTime(now);

        return new ShopStatusResponse(isOpen, nextOpeningTime, shop.TimeZoneId);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static OpeningHoursResponse MapToResponse(Shop shop) =>
        new(shop.OpeningHours
            .OrderBy(b => b.DayOfWeek)
            .ThenBy(b => b.OpenTime)
            .Select(b => new TimeBlockResponse(
                b.Id,
                b.DayOfWeek,
                b.OpenTime.ToString("HH:mm"),
                b.CloseTime.ToString("HH:mm")))
            .ToList());
}
