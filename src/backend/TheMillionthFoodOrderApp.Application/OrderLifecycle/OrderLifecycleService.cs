using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.OrderLifecycle;

public sealed class OrderLifecycleService(
    IOrderLifecycleConfigRepository repository,
    IShopRepository shopRepository) : IOrderLifecycleService
{
    public async Task<OrderLifecycleResponse> GetLifecycleAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var config = await repository.GetByShopIdAsync(shopId, cancellationToken);

        if (config is null)
        {
            config = OrderLifecycleConfig.CreateDefault(shopId);
            await repository.AddAsync(config, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        return MapToResponse(config);
    }

    public async Task<OrderLifecycleResponse> ConfigureLifecycleAsync(
        Guid shopId,
        ConfigureOrderLifecycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var config = await repository.GetByShopIdAsync(shopId, cancellationToken);

        if (config is null)
        {
            config = OrderLifecycleConfig.CreateDefault(shopId);
            await repository.AddAsync(config, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
            // Re-fetch to get a clean tracked entity
            config = (await repository.GetByShopIdAsync(shopId, cancellationToken))!;
        }

        // Build domain entities from the request
        var statuses = request.Statuses
            .Select(s => OrderStatus.Create(
                config.Id, s.Name, s.SystemKey, s.SortOrder, s.IsTerminal, s.ColorHex))
            .ToList();

        // Build a sort-order → status-id lookup to resolve transitions
        var sortOrderToId = statuses.ToDictionary(s => s.SortOrder, s => s.Id);

        var transitions = request.Transitions
            .Select(t => OrderStatusTransition.Create(
                config.Id,
                sortOrderToId[t.FromSortOrder],
                sortOrderToId[t.ToSortOrder]))
            .ToList();

        config.ConfigureLifecycle(statuses, transitions);

        await repository.SaveChangesAsync(cancellationToken);

        return MapToResponse(config);
    }

    public async Task<OrderLifecycleResponse> ResetToDefaultAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var existing = await repository.GetByShopIdAsync(shopId, cancellationToken);
        if (existing is not null)
        {
            await repository.RemoveAsync(existing, cancellationToken);
            await repository.SaveChangesAsync(cancellationToken);
        }

        var config = OrderLifecycleConfig.CreateDefault(shopId);
        await repository.AddAsync(config, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        return MapToResponse(config);
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    private static OrderLifecycleResponse MapToResponse(OrderLifecycleConfig config) =>
        new(
            config.ShopId,
            config.Statuses
                .OrderBy(s => s.SortOrder)
                .Select(s => new OrderStatusResponse(
                    s.Id, s.Name, s.SystemKey, s.SortOrder,
                    s.IsEnabled, s.IsTerminal, s.ColorHex))
                .ToList(),
            config.Transitions
                .Select(t => new OrderStatusTransitionResponse(
                    t.Id, t.FromStatusId, t.ToStatusId))
                .ToList());
}
