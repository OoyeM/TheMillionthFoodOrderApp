using Shouldly;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class ProductTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Test Product", "Beschrijving"),
    ];

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.ShouldNotBeNull();
        product.Id.ShouldNotBe(Guid.Empty);
        product.BasePrice.Amount.ShouldBe(3.50m);
        product.BasePrice.Currency.ShouldBe("EUR");
        product.ImageUrl.ShouldBeNull();
        product.IsDeleted.ShouldBeFalse();
        product.DeletedAt.ShouldBeNull();
        product.Translations.Count.ShouldBe(1);
        product.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Test Product");
    }

    [Fact]
    public void Create_WithMultipleTranslations_IncludesAll()
    {
        var translations = new (string, string, string?)[]
        {
            ("nl", "Friet", "Klein"),
            ("fr", "Frites", "Petit"),
            ("de", "Pommes", "Klein"),
        };

        var product = Product.Create(new Money(3.50m, "EUR"), null, translations);

        product.Translations.Count.ShouldBe(3);
    }

    [Fact]
    public void Create_WithImageUrl_SetsImageUrl()
    {
        var product = Product.Create(
            new Money(3.50m, "EUR"),
            "https://example.com/image.jpg",
            ValidTranslations);

        product.ImageUrl.ShouldBe("https://example.com/image.jpg");
    }

    [Fact]
    public void Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string, string?)>();

        Should.Throw<ArgumentException>(() =>
            Product.Create(new Money(3.50m, "EUR"), null, emptyTranslations));
    }

    [Fact]
    public void Create_RaisesProductCreatedEvent()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        product.DomainEvents.Count.ShouldBe(1);
        product.DomainEvents.ShouldContain(e => e is ProductCreatedEvent);
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (product.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ReplacesAllTranslations()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalCreatedAt = product.CreatedAt;

        var newTranslations = new (string, string, string?)[]
        {
            ("nl", "Updated Name", "Updated desc"),
            ("fr", "Nom Mis à Jour", null),
        };

        product.Update(new Money(5.00m, "EUR"), "https://img.com/new.jpg", newTranslations);

        product.BasePrice.Amount.ShouldBe(5.00m);
        product.ImageUrl.ShouldBe("https://img.com/new.jpg");
        product.Translations.Count.ShouldBe(2);
        product.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Updated Name");
        product.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour");
        product.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Fact]
    public void Update_WithNoTranslations_ThrowsArgumentException()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        Should.Throw<ArgumentException>(() =>
            product.Update(new Money(5.00m, "EUR"), null, Array.Empty<(string, string, string?)>()));
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ClearDomainEvents();

        product.SoftDelete();

        product.IsDeleted.ShouldBeTrue();
        product.DeletedAt.ShouldNotBeNull();
        product.DomainEvents.Count.ShouldBe(1);
        product.DomainEvents.ShouldContain(e => e is ProductDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ClearDomainEvents();

        product.SoftDelete();
        var firstDeletedAt = product.DeletedAt;
        product.ClearDomainEvents();

        product.SoftDelete(); // Second call

        product.IsDeleted.ShouldBeTrue();
        product.DeletedAt.ShouldBe(firstDeletedAt);
        product.DomainEvents.Count.ShouldBe(0); // No duplicate event
    }
}
