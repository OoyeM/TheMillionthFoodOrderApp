namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// Pure domain service that computes the available time slots for a shop
/// for the remainder of today (shop-local time). US-FP-019.
/// </summary>
public static class TimeSlotGenerator
{
    /// <summary>
    /// Generates all future-facing time slots for today based on the shop's
    /// opening hours and time-slot ordering configuration.
    /// </summary>
    /// <param name="shop">The shop whose configuration and opening hours are used.</param>
    /// <param name="now">The current instant in UTC.</param>
    /// <returns>
    /// Ordered list of (Start, End) UTC <see cref="DateTimeOffset"/> pairs.
    /// Returns an empty list when time-slot ordering is disabled, no opening hours exist,
    /// or the time zone identifier is unknown.
    /// </returns>
    /// <remarks>
    /// <para>
    /// The horizon is today only (shop-local calendar day) — no pre-order support.
    /// Worst-case output: a 24 h shop at 5-minute intervals yields 288 slots.
    /// </para>
    /// <para>
    /// Lead time: the first offered slot starts at or after <c>now + interval</c>
    /// (rounded up to the grid anchor), giving the kitchen at least one interval of
    /// prep runway.
    /// </para>
    /// </remarks>
    public static IReadOnlyList<(DateTimeOffset Start, DateTimeOffset End)> GenerateSlotsForToday(
        Shop shop, DateTimeOffset now)
    {
        if (!shop.TimeSlotOrdering.IsEnabled
            || !shop.TimeSlotOrdering.Interval.HasValue
            || shop.OpeningHours.Count == 0)
        {
            return [];
        }

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(shop.TimeZoneId);
        }
        catch (Exception e) when (e is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            return [];
        }

        var intervalMinutes = (int)shop.TimeSlotOrdering.Interval.Value;
        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var todayDow = localNow.DayOfWeek;
        var todayDate = localNow.Date;

        // The earliest slot a customer may pick must start at least `interval` minutes from now.
        var earliestLocalSlotStart = localNow.DateTime.AddMinutes(intervalMinutes);

        var result = new List<(DateTimeOffset Start, DateTimeOffset End)>();

        // Process today's opening-hour blocks only (same-day ordering scope)
        var todaysBlocks = shop.OpeningHours
            .Where(b => b.DayOfWeek == todayDow)
            .OrderBy(b => b.OpenTime);

        foreach (var block in todaysBlocks)
        {
            // Anchor slot grid at the block's open time
            var anchor = todayDate.Add(block.OpenTime.ToTimeSpan());
            // CloseTime > OpenTime is a domain invariant (no overnight blocks), so the
            // block end is always on the same calendar day. Compare full DateTimes —
            // comparing TimeOfDay wraps at midnight and would never terminate the loop.
            var blockEnd = todayDate.Add(block.CloseTime.ToTimeSpan());

            // Step through the grid, checking that each slot:
            //   1. ends at or before CloseTime
            //   2. starts at or after the lead-time threshold
            var slotStart = anchor;

            while (true)
            {
                var slotEnd = slotStart.AddMinutes(intervalMinutes);

                // Slot must fit entirely inside the opening block
                if (slotEnd > blockEnd)
                    break;

                // Skip slots that are too close (lead-time guard)
                if (slotStart >= earliestLocalSlotStart)
                {
                    // Convert local boundaries to UTC DateTimeOffset
                    var utcStart = ConvertLocalToUtc(slotStart, tz);
                    var utcEnd = ConvertLocalToUtc(slotEnd, tz);
                    result.Add((utcStart, utcEnd));
                }

                slotStart = slotEnd; // advance to next slot
            }
        }

        return result.AsReadOnly();
    }

    private static DateTimeOffset ConvertLocalToUtc(DateTime localDateTime, TimeZoneInfo tz)
    {
        var attempt = DateTime.SpecifyKind(localDateTime, DateTimeKind.Unspecified);

        // A local time in the DST clock-forward gap is invalid and ConvertTimeToUtc
        // would throw — step forward to the first valid minute (shifts the slot
        // boundary by at most the gap length, once a year at most).
        for (var i = 0; i < 60 && tz.IsInvalidTime(attempt); i++)
            attempt = attempt.AddMinutes(1);

        // Ambiguous times (clock-back overlap) are interpreted as standard time by
        // ConvertTimeToUtc — no special handling needed.
        var utc = TimeZoneInfo.ConvertTimeToUtc(attempt, tz);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }
}
