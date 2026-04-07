namespace TheMillionthFoodOrderApp.Application.OrderLifecycle;

public interface IOrderLifecycleService
{
    Task<OrderLifecycleResponse> GetLifecycleAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task<OrderLifecycleResponse> ConfigureLifecycleAsync(Guid shopId, ConfigureOrderLifecycleRequest request, CancellationToken cancellationToken = default);
    Task<OrderLifecycleResponse> ResetToDefaultAsync(Guid shopId, CancellationToken cancellationToken = default);
}
