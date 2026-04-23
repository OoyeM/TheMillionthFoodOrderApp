using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class ProductTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Test Product", "Beschrijving"),
    ];

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidData_ReturnsProduct()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product).IsNotNull();
        await Assert.That(product.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(product.BasePrice.Amount).IsEqualTo(3.50m);
        await Assert.That(product.BasePrice.Currency).IsEqualTo("EUR");
        await Assert.That(product.ImageUrl).IsNull();
        await Assert.That(product.IsDeleted).IsFalse();
        await Assert.That(product.DeletedAt).IsNull();
        await Assert.That(product.Translations.Count).IsEqualTo(1);
        await Assert.That(product.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Test Product");
    }

    [Test]
    public async Task Create_WithMultipleTranslations_IncludesAll()
    {
        var translations = new (string, string, string?)[]
        {
            ("nl", "Friet", "Klein"),
            ("fr", "Frites", "Petit"),
            ("de", "Pommes", "Klein"),
        };

        var product = Product.Create(new Money(3.50m, "EUR"), null, translations);

        await Assert.That(product.Translations.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Create_WithImageUrl_SetsImageUrl()
    {
        var product = Product.Create(
            new Money(3.50m, "EUR"),
            "https://example.com/image.jpg",
            ValidTranslations);

        await Assert.That(product.ImageUrl).IsEqualTo("https://example.com/image.jpg");
    }

    [Test]
    public async Task Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string, string?)>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(Product.Create(new Money(3.50m, "EUR"), null, emptyTranslations)));
    }

    [Test]
    public async Task Create_RaisesProductCreatedEvent()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(product.DomainEvents).Contains(e => e is ProductCreatedEvent);
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (product.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_ReplacesAllTranslations()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        var originalCreatedAt = product.CreatedAt;

        var newTranslations = new (string, string, string?)[]
        {
            ("nl", "Updated Name", "Updated desc"),
            ("fr", "Nom Mis à Jour", null),
        };

        product.Update(new Money(5.00m, "EUR"), "https://img.com/new.jpg", newTranslations);

        await Assert.That(product.BasePrice.Amount).IsEqualTo(5.00m);
        await Assert.That(product.ImageUrl).IsEqualTo("https://img.com/new.jpg");
        await Assert.That(product.Translations.Count).IsEqualTo(2);
        await Assert.That(product.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Updated Name");
        await Assert.That(product.Translations).Contains(t => t.LanguageCode == "fr" && t.Name == "Nom Mis à Jour");
        await Assert.That(product.UpdatedAt).IsGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Test]
    public async Task Update_WithNoTranslations_ThrowsArgumentException()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            product.Update(new Money(5.00m, "EUR"), null, Array.Empty<(string, string, string?)>());
            return Task.CompletedTask;
        });
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Test]
    public async Task SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ClearDomainEvents();

        product.SoftDelete();

        await Assert.That(product.IsDeleted).IsTrue();
        await Assert.That(product.DeletedAt).IsNotNull();
        await Assert.That(product.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(product.DomainEvents).Contains(e => e is ProductDeletedEvent);
    }

    [Test]
    public async Task SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);
        product.ClearDomainEvents();

        product.SoftDelete();
        var firstDeletedAt = product.DeletedAt;
        product.ClearDomainEvents();

        product.SoftDelete(); // Second call

        await Assert.That(product.IsDeleted).IsTrue();
        await Assert.That(product.DeletedAt).IsEqualTo(firstDeletedAt);
        await Assert.That(product.DomainEvents.Count).IsEqualTo(0); // No duplicate event
    }

    // ── Allergens & Dietary Tags ─────────────────────────────────────────────

    [Test]
    public async Task Create_WithAllergens_SetsAllergens()
    {
        var allergens = new[] { Allergen.Gluten, Allergen.Milk, Allergen.Eggs };

        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations, allergens);

        await Assert.That(product.Allergens.Count).IsEqualTo(3);
        await Assert.That(product.Allergens).Contains(Allergen.Gluten);
        await Assert.That(product.Allergens).Contains(Allergen.Milk);
        await Assert.That(product.Allergens).Contains(Allergen.Eggs);
    }

    [Test]
    public async Task Create_WithDietaryTags_SetsDietaryTags()
    {
        var dietaryTags = new[] { DietaryTag.Vegan, DietaryTag.GlutenFree };

        var product = Product.Create(
            new Money(3.50m, "EUR"), null, ValidTranslations, dietaryTags: dietaryTags);

        await Assert.That(product.DietaryTags.Count).IsEqualTo(2);
        await Assert.That(product.DietaryTags).Contains(DietaryTag.Vegan);
        await Assert.That(product.DietaryTags).Contains(DietaryTag.GlutenFree);
    }

    [Test]
    public async Task Create_WithNullAllergens_DefaultsToEmpty()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product.Allergens.Count).IsEqualTo(0);
        await Assert.That(product.DietaryTags.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Create_WithDuplicateAllergens_DeduplicatesSilently()
    {
        var allergens = new[] { Allergen.Gluten, Allergen.Gluten, Allergen.Milk };

        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations, allergens);

        await Assert.That(product.Allergens.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Create_WithInvalidAllergenValue_ThrowsArgumentException()
    {
        var invalid = new[] { (Allergen)999 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations, invalid)));
    }

    [Test]
    public async Task Create_WithInvalidDietaryTagValue_ThrowsArgumentException()
    {
        var invalid = new[] { (DietaryTag)42 };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations, dietaryTags: invalid)));
    }

    [Test]
    public async Task Update_ReplacesAllergens()
    {
        var product = Product.Create(
            new Money(3.50m, "EUR"), null, ValidTranslations,
            new[] { Allergen.Gluten, Allergen.Milk });

        product.Update(
            new Money(3.50m, "EUR"), null, ValidTranslations,
            new[] { Allergen.Fish });

        await Assert.That(product.Allergens.Count).IsEqualTo(1);
        await Assert.That(product.Allergens).Contains(Allergen.Fish);
        await Assert.That(product.Allergens).DoesNotContain(Allergen.Gluten);
    }

    [Test]
    public async Task Update_WithNullAllergens_ClearsAllergens()
    {
        var product = Product.Create(
            new Money(3.50m, "EUR"), null, ValidTranslations,
            new[] { Allergen.Gluten });

        product.Update(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.That(product.Allergens.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Update_ReplacesDietaryTags()
    {
        var product = Product.Create(
            new Money(3.50m, "EUR"), null, ValidTranslations,
            dietaryTags: new[] { DietaryTag.Vegan });

        product.Update(
            new Money(3.50m, "EUR"), null, ValidTranslations,
            dietaryTags: new[] { DietaryTag.Halal, DietaryTag.Vegetarian });

        await Assert.That(product.DietaryTags.Count).IsEqualTo(2);
        await Assert.That(product.DietaryTags).Contains(DietaryTag.Halal);
        await Assert.That(product.DietaryTags).DoesNotContain(DietaryTag.Vegan);
    }

    [Test]
    public async Task Update_WithInvalidAllergenValue_ThrowsArgumentException()
    {
        var product = Product.Create(new Money(3.50m, "EUR"), null, ValidTranslations);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            product.Update(
                new Money(3.50m, "EUR"), null, ValidTranslations,
                new[] { (Allergen)(-1) });
            return Task.CompletedTask;
        });
    }
}
