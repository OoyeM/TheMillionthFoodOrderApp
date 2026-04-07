namespace TheMillionthFoodOrderApp.Application.OrderLifecycle;

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed record ConfigureOrderLifecycleRequest(
    List<OrderStatusRequest> Statuses,
    List<OrderStatusTransitionRequest> Transitions);

public sealed record OrderStatusRequest(
    string Name,
    string? SystemKey,
    int SortOrder,
    bool IsTerminal,
    string? ColorHex);

/// <summary>
/// Transitions reference statuses by SortOrder (not by Id) because IDs
/// don't exist yet for newly created statuses.
/// </summary>
public sealed record OrderStatusTransitionRequest(
    int FromSortOrder,
    int ToSortOrder);

// ── Response DTOs ────────────────────────────────────────────────────────────

public sealed record OrderLifecycleResponse(
    Guid ShopId,
    List<OrderStatusResponse> Statuses,
    List<OrderStatusTransitionResponse> Transitions);

public sealed record OrderStatusResponse(
    Guid Id,
    string Name,
    string? SystemKey,
    int SortOrder,
    bool IsEnabled,
    bool IsTerminal,
    string? ColorHex);

public sealed record OrderStatusTransitionResponse(
    Guid Id,
    Guid FromStatusId,
    Guid ToStatusId);
