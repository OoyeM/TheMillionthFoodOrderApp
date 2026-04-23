using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class ShopTests
{
    private static readonly Address ValidAddress =
        new("Vrijdagmarkt", "1", "Gent", "9000");

    private static Shop CreateValidShop() =>
        Shop.Create("Frietjes Gent", "frietjes-gent", ValidAddress, "gent@frietjes.be", "+32 9 123 45 67");

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidData_ReturnsShop()
    {
        var shop = CreateValidShop();

        await Assert.That(shop).IsNotNull();
        await Assert.That(shop.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(shop.Name).IsEqualTo("Frietjes Gent");
        await Assert.That(shop.Slug).IsEqualTo("frietjes-gent");
        await Assert.That(shop.ContactEmail).IsEqualTo("gent@frietjes.be");
        await Assert.That(shop.ContactPhone).IsEqualTo("+32 9 123 45 67");
        await Assert.That(shop.IsActive).IsTrue();
    }

    [Test]
    public async Task Create_DefaultTimeZoneId_IsEuropeBrussels()
    {
        var shop = CreateValidShop();

        await Assert.That(shop.TimeZoneId).IsEqualTo("Europe/Brussels");
    }

    [Test]
    public async Task Create_OpeningHours_IsEmpty()
    {
        var shop = CreateValidShop();

        await Assert.That(shop.OpeningHours.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_RaisesShopCreatedEvent()
    {
        var shop = CreateValidShop();

        await Assert.That(shop.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(shop.DomainEvents).Contains(e => e is ShopCreatedEvent);
    }

    [Test]
    public async Task Create_ShopCreatedEvent_ContainsCorrectData()
    {
        var shop = CreateValidShop();

        var evt = shop.DomainEvents.OfType<ShopCreatedEvent>().Single();
        await Assert.That(evt.ShopId).IsEqualTo(shop.Id);
        await Assert.That(evt.Name).IsEqualTo("Frietjes Gent");
        await Assert.That(evt.Slug).IsEqualTo("frietjes-gent");
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var shop = CreateValidShop();

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (shop.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    // ── UpdateMetadata ────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateMetadata_UpdatesMutableFields()
    {
        var shop = CreateValidShop();
        var newAddress = new Address("Korenmarkt", "2", "Gent", "9000");
        var originalCreatedAt = shop.CreatedAt;

        shop.UpdateMetadata("Frietjes Gent Centrum", newAddress, "centrum@frietjes.be", "+32 9 999 99 99");

        await Assert.That(shop.Name).IsEqualTo("Frietjes Gent Centrum");
        await Assert.That(shop.Address).IsEqualTo(newAddress);
        await Assert.That(shop.ContactEmail).IsEqualTo("centrum@frietjes.be");
        await Assert.That(shop.ContactPhone).IsEqualTo("+32 9 999 99 99");
        await Assert.That(shop.UpdatedAt).IsGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Test]
    public async Task UpdateMetadata_DoesNotChangeSlug()
    {
        var shop = CreateValidShop();
        var originalSlug = shop.Slug;

        shop.UpdateMetadata("Renamed Shop", ValidAddress, "other@frietjes.be", null);

        await Assert.That(shop.Slug).IsEqualTo(originalSlug);
    }

    [Test]
    public async Task UpdateMetadata_WithNullPhone_ClearsPhone()
    {
        var shop = CreateValidShop();

        shop.UpdateMetadata("Frietjes Gent", ValidAddress, "gent@frietjes.be", null);

        await Assert.That(shop.ContactPhone).IsNull();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Test]
    public async Task Deactivate_ActiveShop_FlipsIsActiveToFalse()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();

        await Assert.That(shop.IsActive).IsFalse();
    }

    [Test]
    public async Task Deactivate_ActiveShop_RaisesShopDeactivatedEvent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();

        await Assert.That(shop.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(shop.DomainEvents).Contains(e => e is ShopDeactivatedEvent);
    }

    [Test]
    public async Task Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();
        shop.ClearDomainEvents();

        shop.Deactivate();

        await Assert.That(shop.IsActive).IsFalse();
        await Assert.That(shop.DomainEvents.Count).IsEqualTo(0);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Activate_WhenAlreadyActive_IsIdempotent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Activate();

        await Assert.That(shop.IsActive).IsTrue();
        await Assert.That(shop.DomainEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Activate_OnDeactivatedShop_FlipsIsActiveToTrue()
    {
        var shop = CreateValidShop();
        shop.Deactivate();
        shop.ClearDomainEvents();

        shop.Activate();

        await Assert.That(shop.IsActive).IsTrue();
    }

    // ── SetOpeningHours ───────────────────────────────────────────────────────

    [Test]
    public async Task SetOpeningHours_WithNonOverlappingBlocksSameDay_Succeeds()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0)),
        };

        shop.SetOpeningHours(blocks);

        await Assert.That(shop.OpeningHours.Count).IsEqualTo(2);
    }

    [Test]
    public async Task SetOpeningHours_WithOverlappingBlocksSameDay_ThrowsArgumentException()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(18, 0)),
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            shop.SetOpeningHours(blocks);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task SetOpeningHours_ReplacesAllPreviousEntries()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var firstSchedule = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        };
        shop.SetOpeningHours(firstSchedule);

        var secondSchedule = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Friday, new TimeOnly(10, 0), new TimeOnly(20, 0)),
        };
        shop.SetOpeningHours(secondSchedule);

        await Assert.That(shop.OpeningHours.Count).IsEqualTo(1);
        await Assert.That(shop.OpeningHours).Contains(b => b.DayOfWeek == DayOfWeek.Friday);
        await Assert.That(shop.OpeningHours).DoesNotContain(b => b.DayOfWeek == DayOfWeek.Monday);
        await Assert.That(shop.OpeningHours).DoesNotContain(b => b.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Test]
    public async Task SetOpeningHours_WithEmptyList_ClearsAllHours()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        };
        shop.SetOpeningHours(blocks);

        shop.SetOpeningHours(Array.Empty<OpeningHoursTimeBlock>());

        await Assert.That(shop.OpeningHours.Count).IsEqualTo(0);
    }

    [Test]
    public async Task SetOpeningHours_BlocksOnDifferentDays_NeverOverlap()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        // Same time range but different days — should not throw
        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Tuesday, new TimeOnly(9, 0), new TimeOnly(18, 0)),
        };

        shop.SetOpeningHours(blocks);

        await Assert.That(shop.OpeningHours.Count).IsEqualTo(2);
    }

    // ── IsOpenAt ──────────────────────────────────────────────────────────────

    [Test]
    public async Task IsOpenAt_DuringOpenBlock_ReturnsTrue()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        // Monday block 09:00–17:00 local Brussels time
        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        };
        shop.SetOpeningHours(blocks);

        // 2024-01-08 is a Monday; 11:00 UTC = 12:00 Brussels (CET = UTC+1)
        var openTime = new DateTimeOffset(2024, 1, 8, 11, 0, 0, TimeSpan.Zero);

        await Assert.That(shop.IsOpenAt(openTime)).IsTrue();
    }

    [Test]
    public async Task IsOpenAt_OutsideOpenBlock_ReturnsFalse()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        // Monday block 09:00–17:00 local Brussels time
        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        };
        shop.SetOpeningHours(blocks);

        // 2024-01-08 is a Monday; 19:00 Brussels (UTC+1 in January) = 18:00 UTC
        var closedTime = new DateTimeOffset(2024, 1, 8, 18, 0, 0, TimeSpan.Zero);

        await Assert.That(shop.IsOpenAt(closedTime)).IsFalse();
    }

    [Test]
    public async Task IsOpenAt_WithNoHoursConfigured_ReturnsFalse()
    {
        var shop = CreateValidShop();

        var anyTime = DateTimeOffset.UtcNow;

        await Assert.That(shop.IsOpenAt(anyTime)).IsFalse();
    }
}
