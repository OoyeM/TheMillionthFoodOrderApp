using TheMillionthFoodOrderApp.Domain.MenuCategories;

namespace TheMillionthFoodOrderApp.Tests.Unit.MenuCategories;

public sealed class MenuCategoryTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Starters", "Kleine gerechten"),
    ];

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidData_ReturnsCorrectProperties()
    {
        var category = MenuCategory.Create("https://example.com/image.jpg", 1, ValidTranslations);

        await Assert.That(category).IsNotNull();
        await Assert.That(category.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(category.ImageUrl).IsEqualTo("https://example.com/image.jpg");
        await Assert.That(category.SortOrder).IsEqualTo(1);
        await Assert.That(category.IsDeleted).IsFalse();
        await Assert.That(category.DeletedAt).IsNull();
        await Assert.That(category.Translations.Count).IsEqualTo(1);
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Starters");
    }

    [Test]
    public async Task Create_WithMultipleTranslations_IncludesAll()
    {
        var translations = new (string, string, string?)[]
        {
            ("nl", "Starters", "Kleine gerechten"),
            ("fr", "Entrées", "Petits plats"),
            ("de", "Vorspeisen", "Kleine Gerichte"),
        };

        var category = MenuCategory.Create(null, 0, translations);

        await Assert.That(category.Translations.Count).IsEqualTo(3);
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Starters");
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "fr" && t.Name == "Entrées");
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "de" && t.Name == "Vorspeisen");
    }

    [Test]
    public async Task Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string, string?)>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(MenuCategory.Create(null, 0, emptyTranslations)));
    }

    [Test]
    public async Task Create_RaisesMenuCategoryCreatedEvent()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        await Assert.That(category.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(category.DomainEvents).Contains(e => e is MenuCategoryCreatedEvent);
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (category.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    [Test]
    public async Task Create_WithNullImageUrl_SetsImageUrlToNull()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        await Assert.That(category.ImageUrl).IsNull();
    }

    [Test]
    public async Task Create_SortOrder_IsSetCorrectly()
    {
        var category = MenuCategory.Create(null, 42, ValidTranslations);

        await Assert.That(category.SortOrder).IsEqualTo(42);
    }

    [Test]
    public async Task Create_WithZeroSortOrder_SortOrderIsZero()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        await Assert.That(category.SortOrder).IsEqualTo(0);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Update_ReplacesAllTranslations()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        var originalCreatedAt = category.CreatedAt;

        var newTranslations = new (string, string, string?)[]
        {
            ("nl", "Hoofdgerechten", "Grote porties"),
            ("fr", "Plats principaux", null),
        };

        category.Update("https://example.com/new.jpg", 5, newTranslations);

        await Assert.That(category.ImageUrl).IsEqualTo("https://example.com/new.jpg");
        await Assert.That(category.SortOrder).IsEqualTo(5);
        await Assert.That(category.Translations.Count).IsEqualTo(2);
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Hoofdgerechten");
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "fr" && t.Name == "Plats principaux");
        await Assert.That(category.UpdatedAt).IsGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Test]
    public async Task Update_WithNoTranslations_ThrowsArgumentException()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            category.Update(null, 1, Array.Empty<(string, string, string?)>());
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Update_RemovesOldTranslationsNotInNewList()
    {
        var translations = new (string, string, string?)[]
        {
            ("nl", "Starters", null),
            ("fr", "Entrées", null),
        };
        var category = MenuCategory.Create(null, 0, translations);

        var updatedTranslations = new (string, string, string?)[]
        {
            ("nl", "Gewijzigd", null),
        };
        category.Update(null, 0, updatedTranslations);

        await Assert.That(category.Translations.Count).IsEqualTo(1);
        await Assert.That(category.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Gewijzigd");
        await Assert.That(category.Translations).DoesNotContain(t => t.LanguageCode == "fr");
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Test]
    public async Task SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        category.ClearDomainEvents();

        category.SoftDelete();

        await Assert.That(category.IsDeleted).IsTrue();
        await Assert.That(category.DeletedAt).IsNotNull();
        await Assert.That(category.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(category.DomainEvents).Contains(e => e is MenuCategoryDeletedEvent);
    }

    [Test]
    public async Task SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        category.ClearDomainEvents();

        category.SoftDelete();
        var firstDeletedAt = category.DeletedAt;
        category.ClearDomainEvents();

        category.SoftDelete(); // Second call

        await Assert.That(category.IsDeleted).IsTrue();
        await Assert.That(category.DeletedAt).IsEqualTo(firstDeletedAt);
        await Assert.That(category.DomainEvents.Count).IsEqualTo(0); // No duplicate event
    }

    [Test]
    public async Task SoftDelete_SetsUpdatedAt()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        var beforeDelete = category.UpdatedAt;

        category.SoftDelete();

        await Assert.That(category.UpdatedAt).IsGreaterThanOrEqualTo(beforeDelete);
    }
}
