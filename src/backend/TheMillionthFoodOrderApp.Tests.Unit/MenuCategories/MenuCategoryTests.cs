using Shouldly;
using TheMillionthFoodOrderApp.Domain.MenuCategories;

namespace TheMillionthFoodOrderApp.Tests.Unit.MenuCategories;

public sealed class MenuCategoryTests
{
    private static readonly (string, string, string?)[] ValidTranslations =
    [
        ("nl", "Starters", "Kleine gerechten"),
    ];

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsCorrectProperties()
    {
        var category = MenuCategory.Create("https://example.com/image.jpg", 1, ValidTranslations);

        category.ShouldNotBeNull();
        category.Id.ShouldNotBe(Guid.Empty);
        category.ImageUrl.ShouldBe("https://example.com/image.jpg");
        category.SortOrder.ShouldBe(1);
        category.IsDeleted.ShouldBeFalse();
        category.DeletedAt.ShouldBeNull();
        category.Translations.Count.ShouldBe(1);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Starters");
    }

    [Fact]
    public void Create_WithMultipleTranslations_IncludesAll()
    {
        var translations = new (string, string, string?)[]
        {
            ("nl", "Starters", "Kleine gerechten"),
            ("fr", "Entrées", "Petits plats"),
            ("de", "Vorspeisen", "Kleine Gerichte"),
        };

        var category = MenuCategory.Create(null, 0, translations);

        category.Translations.Count.ShouldBe(3);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Starters");
        category.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Entrées");
        category.Translations.ShouldContain(t => t.LanguageCode == "de" && t.Name == "Vorspeisen");
    }

    [Fact]
    public void Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string, string?)>();

        Should.Throw<ArgumentException>(() =>
            MenuCategory.Create(null, 0, emptyTranslations));
    }

    [Fact]
    public void Create_RaisesMenuCategoryCreatedEvent()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.ShouldContain(e => e is MenuCategoryCreatedEvent);
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (category.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    [Fact]
    public void Create_WithNullImageUrl_SetsImageUrlToNull()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        category.ImageUrl.ShouldBeNull();
    }

    [Fact]
    public void Create_SortOrder_IsSetCorrectly()
    {
        var category = MenuCategory.Create(null, 42, ValidTranslations);

        category.SortOrder.ShouldBe(42);
    }

    [Fact]
    public void Create_WithZeroSortOrder_SortOrderIsZero()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        category.SortOrder.ShouldBe(0);
    }

    // ── Update ────────────────────────────────────────────────────────────────

    [Fact]
    public void Update_ReplacesAllTranslations()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        var originalCreatedAt = category.CreatedAt;

        var newTranslations = new (string, string, string?)[]
        {
            ("nl", "Hoofdgerechten", "Grote porties"),
            ("fr", "Plats principaux", null),
        };

        category.Update("https://example.com/new.jpg", 5, newTranslations);

        category.ImageUrl.ShouldBe("https://example.com/new.jpg");
        category.SortOrder.ShouldBe(5);
        category.Translations.Count.ShouldBe(2);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Hoofdgerechten");
        category.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Plats principaux");
        category.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Fact]
    public void Update_WithNoTranslations_ThrowsArgumentException()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);

        Should.Throw<ArgumentException>(() =>
            category.Update(null, 1, Array.Empty<(string, string, string?)>()));
    }

    [Fact]
    public void Update_RemovesOldTranslationsNotInNewList()
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

        category.Translations.Count.ShouldBe(1);
        category.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Gewijzigd");
        category.Translations.ShouldNotContain(t => t.LanguageCode == "fr");
    }

    // ── SoftDelete ────────────────────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        category.ClearDomainEvents();

        category.SoftDelete();

        category.IsDeleted.ShouldBeTrue();
        category.DeletedAt.ShouldNotBeNull();
        category.DomainEvents.Count.ShouldBe(1);
        category.DomainEvents.ShouldContain(e => e is MenuCategoryDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        category.ClearDomainEvents();

        category.SoftDelete();
        var firstDeletedAt = category.DeletedAt;
        category.ClearDomainEvents();

        category.SoftDelete(); // Second call

        category.IsDeleted.ShouldBeTrue();
        category.DeletedAt.ShouldBe(firstDeletedAt);
        category.DomainEvents.Count.ShouldBe(0); // No duplicate event
    }

    [Fact]
    public void SoftDelete_SetsUpdatedAt()
    {
        var category = MenuCategory.Create(null, 0, ValidTranslations);
        var beforeDelete = category.UpdatedAt;

        category.SoftDelete();

        category.UpdatedAt.ShouldBeGreaterThanOrEqualTo(beforeDelete);
    }
}
