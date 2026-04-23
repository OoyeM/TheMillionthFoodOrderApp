using Shouldly;
using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Tests.Unit.Brands;

public sealed class BrandTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsBrand()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", "+32 9 123 45 67");

        brand.ShouldNotBeNull();
        brand.Id.ShouldNotBe(Guid.Empty);
        brand.Name.ShouldBe("Frietjes");
        brand.Slug.ShouldBe("frietjes");
        brand.ContactEmail.ShouldBe("info@frietjes.be");
        brand.ContactPhone.ShouldBe("+32 9 123 45 67");
        brand.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Create_DatabaseName_IsSlugPrefixed()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.DatabaseName.ShouldBe("brand_frietjes");
    }

    [Fact]
    public void Create_DatabaseName_ContainsSlugAsIs()
    {
        var brand = Brand.Create("My Brand", "my-brand", "info@mybrand.com", null);

        brand.DatabaseName.ShouldBe("brand_my-brand");
        brand.Slug.ShouldBe("my-brand");
    }

    [Fact]
    public void Create_RaisesBrandCreatedEvent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandCreatedEvent);
    }

    [Fact]
    public void Create_BrandCreatedEvent_ContainsCorrectData()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        var evt = brand.DomainEvents.OfType<BrandCreatedEvent>().Single();
        evt.BrandId.ShouldBe(brand.Id);
        evt.Name.ShouldBe("Frietjes");
        evt.Slug.ShouldBe("frietjes");
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (brand.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public void Deactivate_ActiveBrand_FlipsIsActiveToFalse()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();

        brand.IsActive.ShouldBeFalse();
    }

    [Fact]
    public void Deactivate_ActiveBrand_RaisesBrandDeactivatedEvent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();

        brand.DomainEvents.Count.ShouldBe(1);
        brand.DomainEvents.ShouldContain(e => e is BrandDeactivatedEvent);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_IsIdempotent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Deactivate();
        brand.ClearDomainEvents();

        brand.Deactivate();

        brand.IsActive.ShouldBeFalse();
        brand.DomainEvents.Count.ShouldBe(0);
    }

    // ── Activate ──────────────────────────────────────────────────────────────

    [Fact]
    public void Activate_WhenAlreadyActive_IsIdempotent()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.ClearDomainEvents();

        brand.Activate();

        brand.IsActive.ShouldBeTrue();
        brand.DomainEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void Activate_OnDeactivatedBrand_FlipsIsActiveToTrue()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        brand.Deactivate();
        brand.ClearDomainEvents();

        brand.Activate();

        brand.IsActive.ShouldBeTrue();
    }

    [Fact]
    public void Activate_ThenDeactivate_CycleWorks()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.Deactivate();
        brand.IsActive.ShouldBeFalse();

        brand.Activate();
        brand.IsActive.ShouldBeTrue();

        brand.Deactivate();
        brand.IsActive.ShouldBeFalse();
    }

    // ── ConfigureStaffAuth ────────────────────────────────────────────────────

    [Theory]
    [InlineData(StaffAuthMethod.EmailPassword)]
    [InlineData(StaffAuthMethod.GoogleSso)]
    [InlineData(StaffAuthMethod.MicrosoftSso)]
    public void ConfigureStaffAuth_PersistsMethod(StaffAuthMethod method)
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.ConfigureStaffAuth(method);

        brand.StaffAuthMethod.ShouldBe(method);
    }

    [Fact]
    public void Create_DefaultStaffAuthMethod_IsEmailPassword()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.StaffAuthMethod.ShouldBe(StaffAuthMethod.EmailPassword);
    }

    // ── Slug immutability ──────────────────────────────────────────────────────

    [Fact]
    public void Slug_HasNoPublicSetter()
    {
        // Confirm the Slug property has no publicly accessible setter via reflection
        var slugProperty = typeof(Brand).GetProperty(nameof(Brand.Slug));
        slugProperty.ShouldNotBeNull();

        var setter = slugProperty!.SetMethod;
        // Either there is no setter at all, or the setter is non-public (private / protected)
        (setter is null || !setter.IsPublic).ShouldBeTrue();
    }

    // ── UpdateMetadata ────────────────────────────────────────────────────────

    [Fact]
    public void UpdateMetadata_UpdatesNameEmailAndPhone()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);

        brand.UpdateMetadata("Frietjes Updated", "updated@frietjes.be", "+32 9 999 99 99");

        brand.Name.ShouldBe("Frietjes Updated");
        brand.ContactEmail.ShouldBe("updated@frietjes.be");
        brand.ContactPhone.ShouldBe("+32 9 999 99 99");
    }

    [Fact]
    public void UpdateMetadata_DoesNotChangeSlug()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        var originalSlug = brand.Slug;

        brand.UpdateMetadata("Frietjes Renamed", "other@frietjes.be", null);

        brand.Slug.ShouldBe(originalSlug);
    }

    [Fact]
    public void UpdateMetadata_DoesNotChangeDatabaseName()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", null);
        var originalDatabaseName = brand.DatabaseName;

        brand.UpdateMetadata("Frietjes Renamed", "other@frietjes.be", null);

        brand.DatabaseName.ShouldBe(originalDatabaseName);
    }

    [Fact]
    public void UpdateMetadata_WithNullPhone_ClearsPhone()
    {
        var brand = Brand.Create("Frietjes", "frietjes", "info@frietjes.be", "+32 9 123 45 67");

        brand.UpdateMetadata("Frietjes", "info@frietjes.be", null);

        brand.ContactPhone.ShouldBeNull();
    }
}
