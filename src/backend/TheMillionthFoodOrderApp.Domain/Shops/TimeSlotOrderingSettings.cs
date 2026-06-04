using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>Time-slot length in minutes (US-FP-020). Values double as the minute count.</summary>
public enum TimeSlotInterval
{
    FiveMinutes = 5,
    TenMinutes = 10,
    FifteenMinutes = 15,
}

/// <summary>
/// Per-shop time-slot ordering configuration (US-FP-020). When enabled, online orders are
/// placed into fixed-length slots, each capped at <see cref="MaxOrdersPerInterval"/> orders,
/// so order flow matches the kitchen's capacity. When disabled, no slot constraints apply.
/// </summary>
public sealed class TimeSlotOrderingSettings : ValueObject
{
    /// <summary>Whether time-slot ordering is enabled.</summary>
    public bool IsEnabled { get; }

    /// <summary>Slot length. Null when disabled.</summary>
    public TimeSlotInterval? Interval { get; }

    /// <summary>Maximum orders permitted per slot. Null when disabled.</summary>
    public int? MaxOrdersPerInterval { get; }

    // Required by EF Core owned-entity materialisation.
    private TimeSlotOrderingSettings() { }

    private TimeSlotOrderingSettings(bool isEnabled, TimeSlotInterval? interval, int? maxOrdersPerInterval)
    {
        IsEnabled = isEnabled;
        Interval = interval;
        MaxOrdersPerInterval = maxOrdersPerInterval;
    }

    /// <summary>Time-slot ordering turned off (the default for new shops).</summary>
    public static TimeSlotOrderingSettings Disabled() => new(false, null, null);

    /// <summary>
    /// Time-slot ordering turned on with a validated interval and per-slot capacity.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="interval"/> is not a defined value or
    /// <paramref name="maxOrdersPerInterval"/> is not a positive integer.
    /// </exception>
    public static TimeSlotOrderingSettings Enabled(TimeSlotInterval interval, int maxOrdersPerInterval)
    {
        if (!Enum.IsDefined(interval))
            throw new ArgumentException($"'{interval}' is not a valid time-slot interval.", nameof(interval));
        if (maxOrdersPerInterval <= 0)
            throw new ArgumentException("Max orders per interval must be a positive integer.", nameof(maxOrdersPerInterval));

        return new TimeSlotOrderingSettings(true, interval, maxOrdersPerInterval);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return IsEnabled;
        yield return Interval;
        yield return MaxOrdersPerInterval;
    }
}
