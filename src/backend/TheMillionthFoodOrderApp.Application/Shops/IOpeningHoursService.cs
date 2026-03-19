namespace TheMillionthFoodOrderApp.Application.Shops;

public interface IOpeningHoursService
{
    /// <summary>
    /// Replaces the complete opening hours schedule for the given shop.
    /// Clears existing blocks and persists the new schedule atomically.
    /// </summary>
    Task<OpeningHoursResponse> SetOpeningHoursAsync(
        Guid shopId,
        SetOpeningHoursRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>Returns the current opening hours schedule for the given shop.</summary>
    Task<OpeningHoursResponse> GetOpeningHoursAsync(
        Guid shopId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the real-time open/closed status for the given shop,
    /// including the next opening time when currently closed.
    /// </summary>
    Task<ShopStatusResponse> GetShopStatusAsync(
        Guid shopId,
        CancellationToken cancellationToken = default);
}
