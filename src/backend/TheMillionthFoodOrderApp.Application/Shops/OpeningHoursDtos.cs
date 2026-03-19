namespace TheMillionthFoodOrderApp.Application.Shops;

/// <summary>Request to replace all opening hours for a shop.</summary>
public sealed record SetOpeningHoursRequest(List<TimeBlockRequest> TimeBlocks);

/// <summary>A single time block within a weekly opening hours schedule.</summary>
/// <param name="DayOfWeek">Day of week (0 = Sunday, 6 = Saturday).</param>
/// <param name="OpenTime">Opening time in "HH:mm" format (local to the shop's time zone).</param>
/// <param name="CloseTime">Closing time in "HH:mm" format (local to the shop's time zone). Must be after <paramref name="OpenTime"/>.</param>
public sealed record TimeBlockRequest(DayOfWeek DayOfWeek, string OpenTime, string CloseTime);

/// <summary>The full opening hours schedule for a shop.</summary>
public sealed record OpeningHoursResponse(List<TimeBlockResponse> TimeBlocks);

/// <summary>A single persisted time block returned from the API.</summary>
public sealed record TimeBlockResponse(Guid Id, DayOfWeek DayOfWeek, string OpenTime, string CloseTime);

/// <summary>Real-time open/closed status for a shop.</summary>
public sealed record ShopStatusResponse(bool IsOpen, DateTimeOffset? NextOpeningTime, string TimeZoneId);
