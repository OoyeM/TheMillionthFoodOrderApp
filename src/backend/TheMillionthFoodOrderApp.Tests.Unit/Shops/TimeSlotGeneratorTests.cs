using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

/// <summary>
/// Unit tests for <see cref="TimeSlotGenerator"/> (US-FP-019).
/// </summary>
public sealed class TimeSlotGeneratorTests
{
    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal Shop with sensible defaults. Opening hours and time-slot
    /// settings are configured via the Shop domain methods after construction so that
    /// invariants are respected.
    /// </summary>
    private static Shop BuildShop(
        bool slotEnabled = true,
        TimeSlotInterval interval = TimeSlotInterval.FifteenMinutes,
        int maxPerSlot = 4,
        string timeZoneId = "Europe/Brussels")
    {
        // Use the real Shop.Create factory — it validates and assigns a time zone.
        var shop = Shop.Create(
            "Test Shop",
            "test-shop",
            new Address("Street", "1", "City", "1000", "BE"),
            "shop@test.be",
            null);

        // Override the TimeZoneId via the backing field when a non-default TZ is needed for testing.
        if (timeZoneId != "Europe/Brussels")
        {
            // The property has a private setter — access via the compiler-generated backing field.
            var field = typeof(Shop).GetField("<TimeZoneId>k__BackingField",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
            field.SetValue(shop, timeZoneId);
        }

        if (slotEnabled)
            shop.SetTimeSlotOrdering(TimeSlotOrderingSettings.Enabled(interval, maxPerSlot));

        return shop;
    }

    /// <summary>
    /// Adds a single opening-hours block to the shop.
    /// <paramref name="open"/> and <paramref name="close"/> use "HH:mm" format.
    /// </summary>
    private static void AddHours(Shop shop, DayOfWeek day, string open, string close)
    {
        var existing = shop.OpeningHours.ToList();
        existing.Add(OpeningHoursTimeBlock.Create(shop.Id, day, TimeOnly.Parse(open), TimeOnly.Parse(close)));
        shop.SetOpeningHours(existing);
    }

    // ── Disabled / no hours / unknown timezone ────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_WhenDisabled_ReturnsEmpty()
    {
        var shop = BuildShop(slotEnabled: false);
        AddHours(shop, DayOfWeek.Monday, "09:00", "17:00");
        // "now" = Monday at 10:00 local (CEST +02:00)
        var now = new DateTimeOffset(2026, 6, 8, 8, 0, 0, TimeSpan.Zero); // 08:00 UTC = 10:00 CEST

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        await Assert.That(slots).IsEmpty();
    }

    [Test]
    public async Task GenerateSlotsForToday_WhenNoOpeningHours_ReturnsEmpty()
    {
        var shop = BuildShop(); // no hours set
        var now = new DateTimeOffset(2026, 6, 8, 8, 0, 0, TimeSpan.Zero);

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        await Assert.That(slots).IsEmpty();
    }

    [Test]
    public async Task GenerateSlotsForToday_WhenUnknownTimeZone_ReturnsEmpty()
    {
        var shop = BuildShop(timeZoneId: "Mars/Olympus_Mons");
        AddHours(shop, DayOfWeek.Monday, "09:00", "17:00");
        var now = new DateTimeOffset(2026, 6, 8, 8, 0, 0, TimeSpan.Zero);

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        await Assert.That(slots).IsEmpty();
    }

    // ── Grid anchoring ────────────────────────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_SlotsAnchorAtOpenTime()
    {
        // Block 11:30–12:30 (local), 15-min interval, now = 09:00 CEST (07:00 UTC)
        var shop = BuildShop(interval: TimeSlotInterval.FifteenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Monday, "11:30", "12:30");
        var now = new DateTimeOffset(2026, 6, 8, 7, 0, 0, TimeSpan.Zero); // 09:00 CEST

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        // Expected grid (lead time = 15 min; earliest start = 09:15 CEST → well before 11:30)
        // 11:30–11:45, 11:45–12:00, 12:00–12:15, 12:15–12:30 (4 slots)
        await Assert.That(slots.Count).IsEqualTo(4);
        // First slot starts at 11:30 local = 09:30 UTC (CEST is UTC+2)
        var expectedFirstStart = new DateTimeOffset(2026, 6, 8, 9, 30, 0, TimeSpan.Zero);
        await Assert.That(slots[0].Start).IsEqualTo(expectedFirstStart);
    }

    [Test]
    public async Task GenerateSlotsForToday_SlotExceedingCloseTimeExcluded()
    {
        // Block 11:30–11:44, 15-min interval — the single slot would end at 11:45, exceeding close
        var shop = BuildShop(interval: TimeSlotInterval.FifteenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Monday, "11:30", "11:44");
        var now = new DateTimeOffset(2026, 6, 8, 7, 0, 0, TimeSpan.Zero);

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        await Assert.That(slots).IsEmpty();
    }

    // ── Lead time ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_SlotsBeforeLeadTimeExcluded()
    {
        // Block 12:00–14:00, 15-min, now = 12:30 CEST (10:30 UTC)
        // Lead threshold = 12:45 CEST → slots 12:00–12:15, 12:15–12:30, 12:30–12:45 excluded
        // Remaining: 12:45, 13:00, 13:15, 13:30, 13:45
        var shop = BuildShop(interval: TimeSlotInterval.FifteenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Monday, "12:00", "14:00");
        var now = new DateTimeOffset(2026, 6, 8, 10, 30, 0, TimeSpan.Zero); // 12:30 CEST

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        // First available slot should start at 12:45 CEST = 10:45 UTC
        await Assert.That(slots).IsNotEmpty();
        var expectedFirstStart = new DateTimeOffset(2026, 6, 8, 10, 45, 0, TimeSpan.Zero);
        await Assert.That(slots[0].Start).IsEqualTo(expectedFirstStart);
    }

    // ── Two blocks in one day ─────────────────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_TwoBlocksBothCovered()
    {
        // Lunch 12:00–13:00 and Dinner 18:00–20:00, 30-min interval, now = 09:00 CEST
        var shop = BuildShop(interval: TimeSlotInterval.FifteenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Monday, "12:00", "13:00");
        AddHours(shop, DayOfWeek.Monday, "18:00", "20:00");
        var now = new DateTimeOffset(2026, 6, 8, 7, 0, 0, TimeSpan.Zero); // 09:00 CEST

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        // Lunch: 12:00, 12:15, 12:30, 12:45 = 4 slots
        // Dinner: 18:00, 18:15, 18:30, 18:45, 19:00, 19:15, 19:30, 19:45 = 8 slots
        await Assert.That(slots.Count).IsEqualTo(12);

        // No slot should start in the gap between 13:00 and 18:00
        var lunchCloseUtc = new DateTimeOffset(2026, 6, 8, 11, 0, 0, TimeSpan.Zero); // 13:00 CEST
        var dinnerOpenUtc = new DateTimeOffset(2026, 6, 8, 16, 0, 0, TimeSpan.Zero); // 18:00 CEST
        var gapSlots = slots.Where(s => s.Start >= lunchCloseUtc && s.Start < dinnerOpenUtc).ToList();
        await Assert.That(gapSlots).IsEmpty();
    }

    // ── UTC conversion ─────────────────────────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_UtcConversionCorrectForCest()
    {
        // June = CEST = UTC+2
        var shop = BuildShop(interval: TimeSlotInterval.TenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Tuesday, "10:00", "10:20");
        var now = new DateTimeOffset(2026, 6, 9, 6, 0, 0, TimeSpan.Zero); // 08:00 CEST on Tuesday

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        // 10:00 CEST = 08:00 UTC, 10:10 CEST = 08:10 UTC, 10:20 is the close time (excluded)
        await Assert.That(slots.Count).IsEqualTo(2);
        await Assert.That(slots[0].Start).IsEqualTo(new DateTimeOffset(2026, 6, 9, 8, 0, 0, TimeSpan.Zero));
        await Assert.That(slots[0].End).IsEqualTo(new DateTimeOffset(2026, 6, 9, 8, 10, 0, TimeSpan.Zero));
        await Assert.That(slots[1].Start).IsEqualTo(new DateTimeOffset(2026, 6, 9, 8, 10, 0, TimeSpan.Zero));
        await Assert.That(slots[1].End).IsEqualTo(new DateTimeOffset(2026, 6, 9, 8, 20, 0, TimeSpan.Zero));
    }

    // ── No artificial cap ─────────────────────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_LongOpenShopHasManySlots_NoArtificialCap()
    {
        // Verify that no artificial slot cap is applied: a shop open 10:00–22:00 (12 h)
        // at 5-min intervals yields (12*60)/5 = 144 slots (lead-time may cut some near 10:00).
        // If there were an artificial cap of e.g. 48 the assert would catch it.
        var shop = BuildShop(interval: TimeSlotInterval.FiveMinutes, maxPerSlot: 4);
        // Use a non-DST-transition day and a block that doesn't touch midnight
        // Wednesday 2026-07-15 (CEST, UTC+2), block 10:00–22:00
        AddHours(shop, DayOfWeek.Wednesday, "10:00", "22:00");

        // now = 07:00 UTC = 09:00 CEST → lead threshold = 09:05 CEST
        // First slot at 10:00 CEST (>= 09:05), all 144 slots should appear
        var now = new DateTimeOffset(2026, 7, 15, 7, 0, 0, TimeSpan.Zero);

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        // 12 h * 60 min / 5 min = 144 slots
        await Assert.That(slots.Count).IsEqualTo(144);
    }

    // ── Midnight boundary (regression) ───────────────────────────────────────

    [Test]
    public async Task GenerateSlotsForToday_BlockEndingNearMidnight_Terminates()
    {
        // Regression: the loop exit used to compare slotEnd.TimeOfDay (TimeOnly) against
        // CloseTime — when the grid reached midnight, TimeOfDay wrapped to 00:00 and the
        // loop never terminated (DateTime overflow). Block 23:00–23:59, 10-min interval:
        // slots 23:00, 23:10, 23:20, 23:30, 23:40 (23:50–00:00 crosses midnight → excluded).
        var shop = BuildShop(interval: TimeSlotInterval.TenMinutes, maxPerSlot: 4);
        AddHours(shop, DayOfWeek.Monday, "23:00", "23:59");
        var now = new DateTimeOffset(2026, 6, 8, 18, 0, 0, TimeSpan.Zero); // 20:00 CEST

        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        await Assert.That(slots.Count).IsEqualTo(5);
        // Last slot: 23:40–23:50 CEST = 21:40–21:50 UTC
        await Assert.That(slots[^1].Start).IsEqualTo(new DateTimeOffset(2026, 6, 8, 21, 40, 0, TimeSpan.Zero));
        await Assert.That(slots[^1].End).IsEqualTo(new DateTimeOffset(2026, 6, 8, 21, 50, 0, TimeSpan.Zero));
    }
}
