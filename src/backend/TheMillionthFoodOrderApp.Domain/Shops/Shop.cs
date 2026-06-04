using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

public sealed class Shop : AggregateRoot<Guid>, IAuditable
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-safe identifier, unique within the brand's database.
    /// Customer-facing URL pattern: /{brandSlug}/{lang}/shops/{shopSlug}.
    /// Immutable after creation.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// IANA time zone identifier for this shop (e.g. "Europe/Brussels").
    /// Used to interpret <see cref="OpeningHours"/> blocks which are stored as local time.
    /// </summary>
    public string TimeZoneId { get; private set; } = "Europe/Brussels";

    /// <summary>
    /// When true, new orders are automatically printed to the shop's ticket printer
    /// on the kitchen display (US-FP-028). Off by default. Note: the kitchen display
    /// device drives the actual browser print; this flag gates whether it does so.
    /// </summary>
    public bool TicketPrinterEnabled { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<OpeningHoursTimeBlock> _openingHours = [];
    public IReadOnlyCollection<OpeningHoursTimeBlock> OpeningHours => _openingHours.AsReadOnly();

    // Required by EF Core
    private Shop() { }

    /// <summary>
    /// Factory method — the only way to create a valid Shop.
    /// Raises <see cref="ShopCreatedEvent"/> so a future handler can clone the brand's product catalog.
    /// </summary>
    public static Shop Create(
        string name,
        string slug,
        Address address,
        string contactEmail,
        string? contactPhone)
    {
        var now = DateTimeOffset.UtcNow;
        var shop = new Shop
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug,
            Address = address,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        shop.AddDomainEvent(new ShopCreatedEvent(shop.Id, shop.Name, shop.Slug));

        return shop;
    }

    /// <summary>Updates mutable metadata. Slug is intentionally immutable after creation.</summary>
    public void UpdateMetadata(
        string name,
        Address address,
        string contactEmail,
        string? contactPhone)
    {
        Name = name;
        Address = address;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Enables or disables automatic ticket printing for new orders (US-FP-028).
    /// </summary>
    public void SetTicketPrinterEnabled(bool enabled)
    {
        if (TicketPrinterEnabled == enabled)
            return;

        TicketPrinterEnabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Replaces all opening hour time blocks for this shop atomically.
    /// Clears existing blocks and sets new ones from <paramref name="blocks"/>.
    /// Validates that no two blocks on the same day overlap.
    /// </summary>
    /// <param name="blocks">The complete new weekly schedule. May be empty to clear all hours.</param>
    /// <exception cref="ArgumentException">Thrown when any two blocks on the same day overlap.</exception>
    public void SetOpeningHours(IEnumerable<OpeningHoursTimeBlock> blocks)
    {
        var blockList = blocks.ToList();

        // Validate: no overlapping blocks per day
        var byDay = blockList.GroupBy(b => b.DayOfWeek);
        foreach (var group in byDay)
        {
            var sorted = group.OrderBy(b => b.OpenTime).ToList();
            for (var i = 0; i < sorted.Count - 1; i++)
            {
                if (sorted[i].CloseTime > sorted[i + 1].OpenTime)
                    throw new ArgumentException(
                        $"Overlapping time blocks on {group.Key}: " +
                        $"{sorted[i].OpenTime}-{sorted[i].CloseTime} overlaps with {sorted[i + 1].OpenTime}-{sorted[i + 1].CloseTime}.");
            }
        }

        _openingHours.Clear();
        _openingHours.AddRange(blockList);
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Returns whether the shop is open at the given instant.
    /// Converts <paramref name="now"/> to the shop's local time zone and checks against time blocks.
    /// Returns false when no opening hours are configured.
    /// </summary>
    public bool IsOpenAt(DateTimeOffset now)
    {
        if (_openingHours.Count == 0)
            return false;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }

        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        var dayOfWeek = localNow.DayOfWeek;

        return _openingHours.Any(b =>
            b.DayOfWeek == dayOfWeek &&
            b.OpenTime <= localTime &&
            localTime < b.CloseTime);
    }

    /// <summary>
    /// Finds the next time this shop will open after <paramref name="now"/>.
    /// Searches up to 8 days ahead (full week + 1 day buffer) to handle wrap-around.
    /// Returns null when no opening hours are configured.
    /// </summary>
    public DateTimeOffset? GetNextOpeningTime(DateTimeOffset now)
    {
        if (_openingHours.Count == 0)
            return null;

        TimeZoneInfo tz;
        try
        {
            tz = TimeZoneInfo.FindSystemTimeZoneById(TimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }

        var localNow = TimeZoneInfo.ConvertTime(now, tz);
        var localTime = TimeOnly.FromDateTime(localNow.DateTime);
        var todayDow = localNow.DayOfWeek;

        // Search through the current day (remaining blocks) and the next 6 days (7 iterations = full week)
        for (var dayOffset = 0; dayOffset <= 6; dayOffset++)
        {
            var candidateDow = (DayOfWeek)(((int)todayDow + dayOffset) % 7);
            var candidateDate = localNow.Date.AddDays(dayOffset);

            var blocksForDay = _openingHours
                .Where(b => b.DayOfWeek == candidateDow)
                .OrderBy(b => b.OpenTime)
                .ToList();

            foreach (var block in blocksForDay)
            {
                if (dayOffset == 0 && block.OpenTime <= localTime)
                {
                    // This block has already started (or is in the past) today — skip it
                    continue;
                }

                // Found the next opening block; use ConvertTimeToUtc to handle DST correctly
                var openDateTime = candidateDate.Add(block.OpenTime.ToTimeSpan());
                var utcDateTime = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(openDateTime, DateTimeKind.Unspecified), tz);
                return new DateTimeOffset(utcDateTime, TimeSpan.Zero);
            }
        }

        return null;
    }

    /// <summary>
    /// Deactivates this shop, hiding it from customers.
    /// Raises <see cref="ShopDeactivatedEvent"/>.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ShopDeactivatedEvent(Id, Slug));
    }

    /// <summary>Re-activates a previously deactivated shop.</summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
