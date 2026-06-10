using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

/// <summary>
/// Unit tests for <see cref="TimeSlotCalculator"/> (US-FP-019).
/// All tests use a deterministic <c>nowUtc</c> parameter and Europe/Brussels timezone.
/// Brussels is UTC+1 (CET) in winter and UTC+2 (CEST) in summer.
/// </summary>
public sealed class TimeSlotCalculatorTests
{
    private const string BrusselsTimeZone = "Europe/Brussels";

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a shop ID and a single always-open opening block for the given day.
    /// </summary>
    private static OpeningHoursTimeBlock Block(
        DayOfWeek day,
        string openTime,
        string closeTime,
        Guid? shopId = null)
        => OpeningHoursTimeBlock.Create(
            shopId ?? Guid.CreateVersion7(),
            day,
            TimeOnly.Parse(openTime),
            TimeOnly.Parse(closeTime));

    /// <summary>
    /// Builds a Monday 09:00–17:00 block list (shop-local Brussels).
    /// </summary>
    private static IReadOnlyCollection<OpeningHoursTimeBlock> MondayBlock(
        string open = "09:00", string close = "17:00")
        => [Block(DayOfWeek.Monday, open, close)];

    /// <summary>
    /// Creates a UTC DateTimeOffset for a given Brussels local date/time string.
    /// Uses a winter date (non-DST) unless the string already implies a UTC offset.
    /// </summary>
    private static DateTimeOffset BrusselsToUtc(string localDateTime)
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(BrusselsTimeZone);
        var local = DateTime.Parse(localDateTime);
        var utc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(local, DateTimeKind.Unspecified), tz);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    // ── GenerateSlots: alignment ──────────────────────────────────────────────

    [Test]
    public async Task GenerateSlots_FifteenMinuteInterval_AlignedBoundaries()
    {
        // Monday 2025-02-10, now = 09:00 Brussels → first slot is 09:15
        var now = BrusselsToUtc("2025-02-10 09:00:00"); // Monday
        var blocks = MondayBlock("09:00", "10:00");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        // Boundaries aligned at :00/:15/:30/:45 — 09:00 ≤ slot < 10:00, strictly after 09:00
        // → 09:15, 09:30, 09:45
        await Assert.That(slots.Count).IsEqualTo(3);
        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:15");
        await Assert.That(slots[1].LocalLabel).IsEqualTo("09:30");
        await Assert.That(slots[2].LocalLabel).IsEqualTo("09:45");
    }

    [Test]
    public async Task GenerateSlots_TenMinuteInterval_AlignedBoundaries()
    {
        // Monday 2025-02-10, now = 09:00 Brussels
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "09:40");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.TenMinutes, now);

        // :00/:10/:20/:30 — strictly after 09:00 and < 09:40 → 09:10, 09:20, 09:30
        await Assert.That(slots.Count).IsEqualTo(3);
        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:10");
        await Assert.That(slots[1].LocalLabel).IsEqualTo("09:20");
        await Assert.That(slots[2].LocalLabel).IsEqualTo("09:30");
    }

    [Test]
    public async Task GenerateSlots_FiveMinuteInterval_AlignedBoundaries()
    {
        // Monday 2025-02-10, now = 09:00 Brussels
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "09:20");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FiveMinutes, now);

        // :00/:05/:10/:15 — strictly after 09:00 and < 09:20 → 09:05, 09:10, 09:15
        await Assert.That(slots.Count).IsEqualTo(3);
        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:05");
        await Assert.That(slots[1].LocalLabel).IsEqualTo("09:10");
        await Assert.That(slots[2].LocalLabel).IsEqualTo("09:15");
    }

    // ── GenerateSlots: first slot strictly after now ──────────────────────────

    [Test]
    public async Task GenerateSlots_NowExactlyOnBoundary_FirstSlotIsNextBoundary()
    {
        // now = 09:15 exactly — the 09:15 boundary is NOT strictly after now
        var now = BrusselsToUtc("2025-02-10 09:15:00");
        var blocks = MondayBlock("09:00", "10:00");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        // 09:15 is not strictly after 09:15, so first slot = 09:30
        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:30");
    }

    [Test]
    public async Task GenerateSlots_NowBetweenBoundaries_FirstSlotIsNextBoundary()
    {
        // now = 09:10 — next 15-min boundary after 09:10 is 09:15
        var now = BrusselsToUtc("2025-02-10 09:10:00");
        var blocks = MondayBlock("09:00", "10:00");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:15");
    }

    // ── GenerateSlots: block boundary clamping ────────────────────────────────

    [Test]
    public async Task GenerateSlots_SlotsClampToBlockClose()
    {
        // Block closes at 09:30; boundary 09:30 is NOT < CloseTime → excluded
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "09:30");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        // Only 09:15 (< 09:30); 09:30 is excluded
        await Assert.That(slots.Count).IsEqualTo(1);
        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:15");
    }

    [Test]
    public async Task GenerateSlots_NoSlotsBeforeBlockOpen()
    {
        // Block opens at 14:00; now is much earlier
        var now = BrusselsToUtc("2025-02-10 08:00:00"); // Monday
        IReadOnlyCollection<OpeningHoursTimeBlock> blocks = [Block(DayOfWeek.Monday, "14:00", "17:00")];
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        // All returned slots must be >= 14:00
        await Assert.That(slots.All(s => s.LocalLabel.CompareTo("14:00") >= 0)).IsTrue();
        await Assert.That(slots.Count).IsGreaterThan(0);
    }

    // ── GenerateSlots: multiple blocks ────────────────────────────────────────

    [Test]
    public async Task GenerateSlots_MultipleBlocksSameDay_ProducesSlotsInBothBlocks()
    {
        var shopId = Guid.CreateVersion7();
        var now = BrusselsToUtc("2025-02-10 08:00:00"); // Monday, before any block
        var blocks = new List<OpeningHoursTimeBlock>
        {
            Block(DayOfWeek.Monday, "09:00", "10:00", shopId),
            Block(DayOfWeek.Monday, "14:00", "15:00", shopId),
        }.AsReadOnly();

        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        var labels = slots.Select(s => s.LocalLabel).ToList();

        // Should have slots in 09:00–10:00 block and 14:00–15:00 block but not in the gap.
        await Assert.That(labels.Any(l => l.StartsWith("09:"))).IsTrue();
        await Assert.That(labels.Any(l => l.StartsWith("14:"))).IsTrue();
        // No slots in the 10:00–14:00 gap
        await Assert.That(labels.Any(l => string.Compare(l, "10:00", StringComparison.Ordinal) >= 0
                                        && string.Compare(l, "14:00", StringComparison.Ordinal) < 0)).IsFalse();
    }

    [Test]
    public async Task GenerateSlots_GapBetweenBlocks_ProducesNoSlotsInGap()
    {
        var shopId = Guid.CreateVersion7();
        var now = BrusselsToUtc("2025-02-10 11:00:00"); // Monday, in the gap
        var blocks = new List<OpeningHoursTimeBlock>
        {
            Block(DayOfWeek.Monday, "09:00", "11:00", shopId),
            Block(DayOfWeek.Monday, "13:00", "15:00", shopId),
        }.AsReadOnly();

        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        // Morning block 09:00–11:00 has no slots strictly after 11:00.
        // Afternoon block 13:00–15:00 should produce slots from 13:00.
        var labels = slots.Select(s => s.LocalLabel).ToList();
        await Assert.That(labels.Any(l => l.StartsWith("11:") || l.StartsWith("12:"))).IsFalse();
        await Assert.That(labels.Any(l => l.StartsWith("13:"))).IsTrue();
    }

    // ── GenerateSlots: empty / no-hours cases ─────────────────────────────────

    [Test]
    public async Task GenerateSlots_EmptyOpeningHours_ReturnsEmpty()
    {
        var now = BrusselsToUtc("2025-02-10 10:00:00");
        var slots = TimeSlotCalculator.GenerateSlots([], BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);
        await Assert.That(slots.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GenerateSlots_ClosedDay_ReturnsEmpty()
    {
        // All blocks are on Sunday; now is Monday
        var now = BrusselsToUtc("2025-02-10 10:00:00"); // Monday
        IReadOnlyCollection<OpeningHoursTimeBlock> blocks = [Block(DayOfWeek.Sunday, "10:00", "20:00")];
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);
        await Assert.That(slots.Count).IsEqualTo(0);
    }

    [Test]
    public async Task GenerateSlots_UnknownTimeZone_ReturnsEmpty()
    {
        var now = DateTimeOffset.UtcNow;
        var blocks = MondayBlock();
        var slots = TimeSlotCalculator.GenerateSlots(blocks, "Not/ATimeZone", TimeSlotInterval.FifteenMinutes, now);
        await Assert.That(slots.Count).IsEqualTo(0);
    }

    // ── GenerateSlots: label format uses shop-local time ──────────────────────

    [Test]
    public async Task GenerateSlots_LabelsAreShopLocalNotUtc()
    {
        // Monday 2025-02-10 09:15 Brussels = 08:15 UTC (winter, UTC+1).
        // If labels were UTC the first label would be "08:15"; shop-local should be "09:15".
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "10:00");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);

        await Assert.That(slots[0].LocalLabel).IsEqualTo("09:15");
        // Sanity: UTC of first slot should be 08:15 UTC (= 09:15 Brussels - 1h)
        await Assert.That(slots[0].SlotStartUtc.UtcDateTime.Hour).IsEqualTo(8);
        await Assert.That(slots[0].SlotStartUtc.UtcDateTime.Minute).IsEqualTo(15);
    }

    // ── GenerateSlots: DST transition ─────────────────────────────────────────

    [Test]
    public async Task GenerateSlots_DstTransitionDay_DoesNotCrash()
    {
        // Last Sunday of March 2025 is 2025-03-30: clocks spring forward 02:00→03:00 (Brussels).
        // now = 01:30 Brussels local time on DST day.
        // Slots in the 02:00–03:00 window are technically invalid/ambiguous; we just check
        // that the method returns a valid (possibly empty) list without throwing.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(BrusselsTimeZone);
        var localDstDay = new DateTime(2025, 3, 30, 1, 30, 0);
        var utcNow = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(localDstDay, DateTimeKind.Unspecified), tz),
            TimeSpan.Zero);

        // Block that spans across the DST gap: 01:00–04:00
        IReadOnlyCollection<OpeningHoursTimeBlock> blocks = [Block(DayOfWeek.Sunday, "01:00", "04:00")];

        // Should not throw — DST-invalid boundaries are skipped internally.
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, utcNow);
        await Assert.That(slots).IsNotNull();
    }

    // ── IsValidSlotStart: accepts offered slot ────────────────────────────────

    [Test]
    public async Task IsValidSlotStart_OfferedSlot_ReturnsTrue()
    {
        // Generate slots for Monday 09:00–17:00, take the first one and validate it.
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "17:00");
        var slots = TimeSlotCalculator.GenerateSlots(blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, now);
        await Assert.That(slots.Count).IsGreaterThan(0);

        var firstSlot = slots[0].SlotStartUtc;
        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, firstSlot, now);

        await Assert.That(result).IsTrue();
    }

    // ── IsValidSlotStart: design decision 10 — started-but-not-elapsed ───────

    [Test]
    public async Task IsValidSlotStart_SlotStartedButNotElapsed_ReturnsTrue()
    {
        // Customer picked 17:15 at 17:10, submits at 17:16. Slot 17:15–17:30, not yet fully elapsed.
        var now = BrusselsToUtc("2025-02-10 17:16:00"); // Monday
        var blocks = MondayBlock("09:00", "20:00");

        // Slot start = 17:15 Brussels = 16:15 UTC (winter)
        var slotStart = BrusselsToUtc("2025-02-10 17:15:00");

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsTrue();
    }

    // ── IsValidSlotStart: rejects fully elapsed slot ──────────────────────────

    [Test]
    public async Task IsValidSlotStart_FullyElapsedSlot_ReturnsFalse()
    {
        // now = 17:31 Brussels; slot 17:15, interval 15 min → window ends 17:30 < now
        var now = BrusselsToUtc("2025-02-10 17:31:00"); // Monday
        var blocks = MondayBlock("09:00", "20:00");
        var slotStart = BrusselsToUtc("2025-02-10 17:15:00");

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: rejects misaligned slot ────────────────────────────

    [Test]
    public async Task IsValidSlotStart_MisalignedSlot_ReturnsFalse()
    {
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "17:00");
        // 09:17 is not aligned to 15-minute boundaries
        var slotStart = BrusselsToUtc("2025-02-10 09:17:00");

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: rejects sub-minute components ──────────────────────

    [Test]
    public async Task IsValidSlotStart_SlotWithSeconds_ReturnsFalse()
    {
        // 09:15:30 passes a minute-granularity alignment check but would be stored as its
        // own capacity bucket, bypassing MaxOrdersPerInterval — must be rejected outright.
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "17:00");
        var slotStart = BrusselsToUtc("2025-02-10 09:15:30");

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task IsValidSlotStart_SlotWithMilliseconds_ReturnsFalse()
    {
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "17:00");
        var slotStart = BrusselsToUtc("2025-02-10 09:15:00").AddMilliseconds(500);

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: rejects slot outside opening block ─────────────────

    [Test]
    public async Task IsValidSlotStart_OutsideOpeningBlock_ReturnsFalse()
    {
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var blocks = MondayBlock("09:00", "17:00");
        // 18:00 is after close
        var slotStart = BrusselsToUtc("2025-02-10 18:00:00");

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: rejects aligned slot on a future weekday ──────────

    [Test]
    public async Task IsValidSlotStart_AlignedSlotNextWeekSameWeekday_ReturnsFalse()
    {
        // now = Monday 2025-02-10; slot is Monday 2025-02-17 (7 days ahead, same weekday).
        // Opening-hours blocks are weekday-keyed so block-containment alone would pass.
        // The same-local-day check must catch this.
        var now = BrusselsToUtc("2025-02-10 09:00:00");  // Monday Feb 10
        var blocks = MondayBlock("09:00", "17:00");
        var futureSlot = BrusselsToUtc("2025-02-17 09:15:00"); // Monday Feb 17

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, futureSlot, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: unknown timezone ───────────────────────────────────

    [Test]
    public async Task IsValidSlotStart_UnknownTimeZone_ReturnsFalse()
    {
        var now = DateTimeOffset.UtcNow;
        var blocks = MondayBlock();
        var slotStart = now.AddMinutes(15);

        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, "Not/ATimeZone", TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }

    // ── IsValidSlotStart: DST transition date ────────────────────────────────

    [Test]
    public async Task IsValidSlotStart_DstTransitionDay_DoesNotCrash()
    {
        // 2025-03-30 Brussels DST day (spring forward at 02:00). Use a safe post-transition slot.
        var tz = TimeZoneInfo.FindSystemTimeZoneById(BrusselsTimeZone);
        var localNow = new DateTime(2025, 3, 30, 10, 0, 0);
        var utcNow = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localNow, DateTimeKind.Unspecified), tz),
            TimeSpan.Zero);

        var localSlot = new DateTime(2025, 3, 30, 10, 15, 0);
        var utcSlot = new DateTimeOffset(
            TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(localSlot, DateTimeKind.Unspecified), tz),
            TimeSpan.Zero);

        IReadOnlyCollection<OpeningHoursTimeBlock> blocks = [Block(DayOfWeek.Sunday, "09:00", "20:00")];

        // Should not throw and the post-DST-transition slot should be valid.
        var result = TimeSlotCalculator.IsValidSlotStart(
            blocks, BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, utcSlot, utcNow);
        await Assert.That(result).IsTrue();
    }

    // ── IsValidSlotStart: empty opening hours ────────────────────────────────

    [Test]
    public async Task IsValidSlotStart_EmptyOpeningHours_ReturnsFalse()
    {
        var now = BrusselsToUtc("2025-02-10 09:00:00");
        var slotStart = BrusselsToUtc("2025-02-10 09:15:00");

        var result = TimeSlotCalculator.IsValidSlotStart(
            [], BrusselsTimeZone, TimeSlotInterval.FifteenMinutes, slotStart, now);

        await Assert.That(result).IsFalse();
    }
}
