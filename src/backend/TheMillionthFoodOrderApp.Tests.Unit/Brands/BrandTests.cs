using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Tests.Unit.Brands;

public sealed class BrandTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidData_ReturnsBrand()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", "+32 9 123 45 67");

        await Assert.That(brand).IsNotNull();
        await Assert.That(brand.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(brand.Name).IsEqualTo("Frietjes");
        await Assert.That(brand.Slug).IsEqualTo("frietjes");
        await Assert.That(brand.ContactEmail).IsEqualTo("info@frietjes.be");
        await Assert.That(brand.ContactPhone).IsEqualTo("+32 9 123 45 67");
        await Assert.That(brand.IsActive).IsTrue();
    }

    [Test]
    public async Task Create_DatabaseName_IsSlugPrefixed()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        await Assert.That(brand.DatabaseName).IsEqualTo("brand_frietjes");
    }

    [Test]
    public async Task Create_DatabaseName_ContainsSlugAsIs()
    {
        var brand = Brand.Create("My Brand", "my-brand", "info@mybrand.com", null);

        await Assert.That(brand.DatabaseName).IsEqualTo("brand_my-brand");
        await Assert.That(brand.Slug).IsEqualTo("my-brand");
    }

    [Test]
    public async Task Create_RaisesBrandCreatedEvent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        await Assert.That(brand.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(brand.DomainEvents).Contains(e => e is BrandCreatedEvent);
    }

    [Test]
    public async Task Create_BrandCreatedEvent_ContainsCorrectData()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        var evt = brand.DomainEvents.OfType<BrandCreatedEvent>().Single();
        await Assert.That(evt.BrandId).IsEqualTo(brand.Id);
        await Assert.That(evt.Name).IsEqualTo("Frietjes");
        await Assert.That(evt.Slug).IsEqualTo("frietjes");
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (brand.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Test]
    public async Task Deactivate_ActiveBrand_FlipsIsActiveToFalse()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();

        await Assert.That(brand.IsActive).IsFalse();
    }

    [Test]
    public async Task Deactivate_ActiveBrand_RaisesBrandDeactivatedEvent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();

        await Assert.That(brand.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(brand.DomainEvents).Contains(e => e is BrandDeactivatedEvent);
    }

    [Test]
    public async Task Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();
        brand.ClearDomainEvents();

        brand.Deactivate();

        await Assert.That(brand.IsActive).IsFalse();
        await Assert.That(brand.DomainEvents.Count).IsEqualTo(0);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Activate_WhenAlreadyActive_IsIdempotent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Activate();

        await Assert.That(brand.IsActive).IsTrue();
        await Assert.That(brand.DomainEvents.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Activate_OnDeactivatedBrand_FlipsIsActiveToTrue()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.Deactivate();
        brand.ClearDomainEvents();

        brand.Activate();

        await Assert.That(brand.IsActive).IsTrue();
    }

    [Test]
    public async Task Activate_ThenDeactivate_CycleWorks()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.Deactivate();
        await Assert.That(brand.IsActive).IsFalse();

        brand.Activate();
        await Assert.That(brand.IsActive).IsTrue();

        brand.Deactivate();
        await Assert.That(brand.IsActive).IsFalse();
    }

    // ── ConfigureStaffAuth ────────────────────────────────────────────────────

    [Arguments(StaffAuthMethod.EmailPassword)]
    [Arguments(StaffAuthMethod.GoogleSso)]
    [Arguments(StaffAuthMethod.MicrosoftSso)]
    [Test]
    public async Task ConfigureStaffAuth_PersistsMethod(StaffAuthMethod method)
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.ConfigureStaffAuth(method);

        await Assert.That(brand.StaffAuthMethod).IsEqualTo(method);
    }

    [Test]
    public async Task Create_DefaultStaffAuthMethod_IsEmailPassword()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        await Assert.That(brand.StaffAuthMethod).IsEqualTo(StaffAuthMethod.EmailPassword);
    }

    // ── Slug immutability ──────────────────────────────────────────────────────

    [Test]
    public async Task Slug_HasNoPublicSetter()
    {
        // Confirm the Slug property has no publicly accessible setter via reflection
        var slugProperty = typeof(Brand).GetProperty(nameof(Brand.Slug));
        await Assert.That(slugProperty).IsNotNull();

        var setter = slugProperty!.SetMethod;
        // Either there is no setter at all, or the setter is non-public (private / protected)
        await Assert.That(setter is null || !setter.IsPublic).IsTrue();
    }

    // ── UpdateMetadata ────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateMetadata_UpdatesNameEmailAndPhone()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.UpdateMetadata("Frietjes Updated", "updated@frietjes.be", "+32 9 999 99 99");

        await Assert.That(brand.Name).IsEqualTo("Frietjes Updated");
        await Assert.That(brand.ContactEmail).IsEqualTo("updated@frietjes.be");
        await Assert.That(brand.ContactPhone).IsEqualTo("+32 9 999 99 99");
    }

    [Test]
    public async Task UpdateMetadata_DoesNotChangeSlug()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        var originalSlug = brand.Slug;

        brand.UpdateMetadata("Frietjes Renamed", "other@frietjes.be", null);

        await Assert.That(brand.Slug).IsEqualTo(originalSlug);
    }

    [Test]
    public async Task UpdateMetadata_DoesNotChangeDatabaseName()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        var originalDatabaseName = brand.DatabaseName;

        brand.UpdateMetadata("Frietjes Renamed", "other@frietjes.be", null);

        await Assert.That(brand.DatabaseName).IsEqualTo(originalDatabaseName);
    }

    [Test]
    public async Task UpdateMetadata_WithNullPhone_ClearsPhone()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", "+32 9 123 45 67");

        brand.UpdateMetadata("Frietjes", "info@frietjes.be", null);

        await Assert.That(brand.ContactPhone).IsNull();
    }
}
