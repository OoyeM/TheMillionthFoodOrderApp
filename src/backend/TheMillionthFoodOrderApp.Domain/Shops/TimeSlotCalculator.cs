namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// A single candidate slot returned by <see cref="TimeSlotCalculator.GenerateSlots"/>.
/// </summary>
/// <param name="SlotStartUtc">The slot boundary in UTC.</param>
/// <param name="LocalLabel">Shop-local start time formatted as <c>"HH:mm"</c>.</param>
public readonly record struct TimeSlotCandidate(DateTimeOffset SlotStartUtc, string LocalLabel);

/// <summary>
/// Pure domain service for time-slot arithmetic (US-FP-019).
/// All operations are fully deterministic given explicit inputs — no clock abstraction needed.
/// Call sites in the Application layer supply <c>DateTimeOffset.UtcNow</c>.
/// </summary>
public static class TimeSlotCalculator
{
    /// <summary>
    /// Generates available slot start boundaries for the shop-local day containing
    /// <paramref name="nowUtc"/>.
    /// <para>
    /// Boundaries are aligned to multiples of <paramref name="interval"/> from the hour in
    /// shop-local minutes-since-midnight (e.g. 15-minute interval → :00, :15, :30, :45).
    /// Only boundaries strictly after <paramref name="nowUtc"/> and inside an opening block
    /// of that day are returned.
    /// </para>
    /// <para>
    /// Unknown time zone or empty opening hours → empty list.
    /// </para>
    /// </summary>
    public static IReadOnlyList<TimeSlotCandidate> GenerateSlots(
        IReadOnlyCollection<OpeningHoursTimeBlock> openingHours,
        string timeZoneId,
        TimeSlotInterval interval,
        DateTimeOffset nowUtc)
    {
        if (openingHours.Count == 0)
            return [];

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return [];
        }

        var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);
        var localDate = localNow.Date;
        var todayDow = localNow.DayOfWeek;
        var intervalMinutes = (int)interval;

        // Collect all opening blocks for today, sorted by open time.
        var todayBlocks = openingHours
            .Where(b => b.DayOfWeek == todayDow)
            .OrderBy(b => b.OpenTime)
            .ToList();

        if (todayBlocks.Count == 0)
            return [];

        var candidates = new List<TimeSlotCandidate>();

        foreach (var block in todayBlocks)
        {
            // Iterate aligned boundaries from block open up to (but not including) block close.
            // Alignment: minutes-since-midnight divisible by interval.
            var blockOpenMinutes = block.OpenTime.Hour * 60 + block.OpenTime.Minute;
            var blockCloseMinutes = block.CloseTime.Hour * 60 + block.CloseTime.Minute;

            // First boundary >= block open aligned to interval
            var firstBoundaryMinutes = blockOpenMinutes % intervalMinutes == 0
                ? blockOpenMinutes
                : blockOpenMinutes + (intervalMinutes - blockOpenMinutes % intervalMinutes);

            for (var boundaryMinutes = firstBoundaryMinutes;
                 boundaryMinutes < blockCloseMinutes;
                 boundaryMinutes += intervalMinutes)
            {
                var localStart = localDate.AddMinutes(boundaryMinutes);

                // Convert boundary to UTC via Unspecified-kind pattern (DST-safe; per-boundary).
                DateTime utcDateTime;
                try
                {
                    utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
                        DateTime.SpecifyKind(localStart, DateTimeKind.Unspecified), tz);
                }
                catch (ArgumentException)
                {
                    // The local time is invalid (skipped by DST spring-forward) — skip this boundary.
                    continue;
                }

                var slotStartUtc = new DateTimeOffset(utcDateTime, TimeSpan.Zero);

                // Only boundaries strictly after now.
                if (slotStartUtc <= nowUtc)
                    continue;

                var label = localStart.ToString("HH\\:mm");
                candidates.Add(new TimeSlotCandidate(slotStartUtc, label));
            }
        }

        return candidates.AsReadOnly();
    }

    /// <summary>
    /// Create-time gate for a client-submitted slot.
    /// Returns <see langword="true"/> when <paramref name="slotStartUtc"/>:
    /// <list type="bullet">
    ///   <item>converts back to an interval-aligned shop-local boundary,</item>
    ///   <item>falls within an opening block on the same shop-local day as <paramref name="nowUtc"/>,</item>
    ///   <item>and its window has not yet fully elapsed
    ///         (<c>slotStartUtc + interval &gt; nowUtc</c> — design decision 10).</item>
    /// </list>
    /// Unknown time zone → <see langword="false"/>.
    /// </summary>
    public static bool IsValidSlotStart(
        IReadOnlyCollection<OpeningHoursTimeBlock> openingHours,
        string timeZoneId,
        TimeSlotInterval interval,
        DateTimeOffset slotStartUtc,
        DateTimeOffset nowUtc)
    {
        if (openingHours.Count == 0)
            return false;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }

        var intervalMinutes = (int)interval;

        // Design decision 10: slot window must not be fully elapsed.
        if (slotStartUtc.AddMinutes(intervalMinutes) <= nowUtc)
            return false;

        // Convert submitted slot to shop-local time using DST-safe conversion.
        var localSlot = TimeZoneInfo.ConvertTime(slotStartUtc, tz);
        var localSlotTime = TimeOnly.FromDateTime(localSlot.DateTime);
        var slotDow = localSlot.DayOfWeek;
        var slotLocalDate = localSlot.Date;

        // Same-local-day check: the slot must be on the same local calendar date as now.
        // This prevents accepting aligned boundaries on future weekdays that share the same day-of-week.
        var localNow = TimeZoneInfo.ConvertTime(nowUtc, tz);
        if (slotLocalDate != localNow.Date)
            return false;

        // Alignment check at tick precision: the boundary must land exactly on a multiple of the
        // interval from local midnight. Sub-minute components (e.g. 17:15:30) would otherwise be
        // stored as their own capacity bucket and bypass MaxOrdersPerInterval entirely.
        if (localSlot.TimeOfDay.Ticks % (intervalMinutes * TimeSpan.TicksPerMinute) != 0)
            return false;

        // Opening-block containment: slot start must be within an opening block (open ≤ slot < close).
        var inBlock = openingHours.Any(b =>
            b.DayOfWeek == slotDow &&
            b.OpenTime <= localSlotTime &&
            localSlotTime < b.CloseTime);

        return inBlock;
    }
}
