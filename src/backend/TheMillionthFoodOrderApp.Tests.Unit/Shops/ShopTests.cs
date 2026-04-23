using Shouldly;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class ShopTests
{
    private static readonly Address ValidAddress =
        new("Vrijdagmarkt", "1", "Gent", "9000");

    private static Shop CreateValidShop() =>
        Shop.Create("Frietjes Gent", "frietjes-gent", ValidAddress, "gent@frietjes.be", "+32 9 123 45 67");

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsShop()
    {
        var shop = CreateValidShop();

        shop.ShouldNotBeNull();
        shop.Id.ShouldNotBe(Guid.Empty);
        shop.Name.ShouldBe("Frietjes Gent");
        shop.Slug.ShouldBe("frietjes-gent");
        shop.ContactEmail.ShouldBe("gent@frietjes.be");
        shop.ContactPhone.ShouldBe("+32 9 123 45 67");
        shop.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_DefaultTimeZoneId_IsEuropeBrussels()
    {
        var shop = CreateValidShop();

        shop.TimeZoneId.ShouldBe("Europe/Brussels");
    }

    [Fact]
    public void Create_OpeningHours_IsEmpty()
    {
        var shop = CreateValidShop();

        shop.OpeningHours.Count.ShouldBe(0);
    }

    [Fact]
    public void Create_RaisesShopCreatedEvent()
    {
        var shop = CreateValidShop();

        shop.DomainEvents.Count.ShouldBe(1);
        shop.DomainEvents.ShouldContain(e => e is ShopCreatedEvent);
    }

    [Fact]
    public void Create_ShopCreatedEvent_ContainsCorrectData()
    {
        var shop = CreateValidShop();

        var evt = shop.DomainEvents.OfType<ShopCreatedEvent>().Single();
        evt.ShopId.ShouldBe(shop.Id);
        evt.Name.ShouldBe("Frietjes Gent");
        evt.Slug.ShouldBe("frietjes-gent");
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var shop = CreateValidShop();

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (shop.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    // ── UpdateMetadata ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMetadata_UpdatesMutableFields()
    {
        var shop = CreateValidShop();
        var newAddress = new Address("Korenmarkt", "2", "Gent", "9000");
        var originalCreatedAt = shop.CreatedAt;

        shop.UpdateMetadata("Frietjes Gent Centrum", newAddress, "centrum@frietjes.be", "+32 9 999 99 99");

        shop.Name.ShouldBe("Frietjes Gent Centrum");
        shop.Address.ShouldBe(newAddress);
        shop.ContactEmail.ShouldBe("centrum@frietjes.be");
        shop.ContactPhone.ShouldBe("+32 9 999 99 99");
        shop.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Fact]
    public void UpdateMetadata_DoesNotChangeSlug()
    {
        var shop = CreateValidShop();
        var originalSlug = shop.Slug;

        shop.UpdateMetadata("Renamed Shop", ValidAddress, "other@frietjes.be", null);

        shop.Slug.ShouldBe(originalSlug);
    }

    [Fact]
    public void UpdateMetadata_WithNullPhone_ClearsPhone()
    {
        var shop = CreateValidShop();

        shop.UpdateMetadata("Frietjes Gent", ValidAddress, "gent@frietjes.be", null);

        shop.ContactPhone.ShouldBeNull();
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveShop_FlipsIsActiveToFalse()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();

        shop.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_ActiveShop_RaisesShopDeactivatedEvent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();

        shop.DomainEvents.Count.ShouldBe(1);
        shop.DomainEvents.ShouldContain(e => e is ShopDeactivatedEvent);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Deactivate();
        shop.ClearDomainEvents();

        shop.Deactivate();

        shop.IsActive.ShouldBeFalse();
        shop.DomainEvents.Count.ShouldBe(0);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var shop = CreateValidShop();
        shop.ClearDomainEvents();

        shop.Activate();

        shop.IsActive.ShouldBeTrue();
        shop.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void Activate_OnDeactivatedShop_FlipsIsActiveToTrue()
    {
        var shop = CreateValidShop();
        shop.Deactivate();
        shop.ClearDomainEvents();

        shop.Activate();

        shop.IsActive.ShouldBeTrue();
    }

    // ── SetOpeningHours ───────────────────────────────────────────────────────

    [Fact]
    public void SetOpeningHours_WithNonOverlappingBlocksSameDay_Succeeds()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(12, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(14, 0), new TimeOnly(18, 0)),
        };

        shop.SetOpeningHours(blocks);

        shop.OpeningHours.Count.ShouldBe(2);
    }

    [Fact]
    public void SetOpeningHours_WithOverlappingBlocksSameDay_ThrowsArgumentException()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(13, 0)),
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(12, 0), new TimeOnly(18, 0)),
        };

        Should.Throw<ArgumentException>(() => shop.SetOpeningHours(blocks));
    }

    [Fact]
    public void SetOpeningHours_ReplacesAllPreviousEntries()
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

        shop.OpeningHours.Count.ShouldBe(1);
        shop.OpeningHours.ShouldContain(b => b.DayOfWeek == DayOfWeek.Friday);
        shop.OpeningHours.ShouldNotContain(b => b.DayOfWeek == DayOfWeek.Monday);
        shop.OpeningHours.ShouldNotContain(b => b.DayOfWeek == DayOfWeek.Tuesday);
    }

    [Fact]
    public void SetOpeningHours_WithEmptyList_ClearsAllHours()
    {
        var shop = CreateValidShop();
        var shopId = shop.Id;

        var blocks = new[]
        {
            OpeningHoursTimeBlock.Create(shopId, DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(17, 0)),
        };
        shop.SetOpeningHours(blocks);

        shop.SetOpeningHours(Array.Empty<OpeningHoursTimeBlock>());

        shop.OpeningHours.Count.ShouldBe(0);
    }

    [Fact]
    public void SetOpeningHours_BlocksOnDifferentDays_NeverOverlap()
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

        shop.OpeningHours.Count.ShouldBe(2);
    }

    // ── IsOpenAt ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsOpenAt_DuringOpenBlock_ReturnsTrue()
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

        shop.IsOpenAt(openTime).ShouldBeTrue();
    }

    [Fact]
    public void IsOpenAt_OutsideOpenBlock_ReturnsFalse()
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

        shop.IsOpenAt(closedTime).ShouldBeFalse();
    }

    [Fact]
    public void IsOpenAt_WithNoHoursConfigured_ReturnsFalse()
    {
        var shop = CreateValidShop();

        var anyTime = DateTimeOffset.UtcNow;

        shop.IsOpenAt(anyTime).ShouldBeFalse();
    }
}
