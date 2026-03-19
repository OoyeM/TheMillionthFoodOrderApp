using Shouldly;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;

namespace TheMillionthFoodOrderApp.Tests.Unit.ModifierGroups;

public sealed class ModifierGroupTests
{
    private static readonly (string languageCode, string name)[] ValidTranslations =
    [
        ("nl", "Sauzen"),
    ];

    private static readonly (decimal priceAdjustment, int sortOrder, IEnumerable<(string languageCode, string name)> translations)[] ValidModifiers =
    [
        (0.00m, 0, [("nl", "Mayonaise")]),
        (0.50m, 1, [("nl", "Ketchup")]),
    ];

    // ── ModifierGroup.Create ───────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsModifierGroup()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        group.ShouldNotBeNull();
        group.Id.ShouldNotBe(Guid.Empty);
        group.IsDeleted.ShouldBeFalse();
        group.DeletedAt.ShouldBeNull();
        group.Translations.Count.ShouldBe(1);
        group.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Sauzen");
        group.Modifiers.Count.ShouldBe(2);
    }

    [Fact]
    public void Create_WithMultipleModifiers_IncludesAll()
    {
        var modifiers = new (decimal, int, IEnumerable<(string, string)>)[]
        {
            (0.00m, 0, [("nl", "Klein")]),
            (0.50m, 1, [("nl", "Middel")]),
            (1.00m, 2, [("nl", "Groot")]),
        };

        var group = ModifierGroup.Create(ValidTranslations, modifiers);

        group.Modifiers.Count.ShouldBe(3);
    }

    [Fact]
    public void Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string)>();

        Should.Throw<ArgumentException>(() =>
            ModifierGroup.Create(emptyTranslations, ValidModifiers));
    }

    [Fact]
    public void Create_RaisesModifierGroupCreatedEvent()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        group.DomainEvents.Count.ShouldBe(1);
        group.DomainEvents.ShouldContain(e => e is ModifierGroupCreatedEvent);
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (group.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    [Fact]
    public void Create_AssignsSequentialSortOrderToModifiers()
    {
        var modifiers = new (decimal, int, IEnumerable<(string, string)>)[]
        {
            (0.00m, 0, [("nl", "Klein")]),
            (0.50m, 1, [("nl", "Middel")]),
            (1.00m, 2, [("nl", "Groot")]),
        };

        var group = ModifierGroup.Create(ValidTranslations, modifiers);

        var sortOrders = group.Modifiers.Select(m => m.SortOrder).OrderBy(s => s).ToList();
        sortOrders.ShouldBe([0, 1, 2]);
    }

    // ── ModifierGroup.Update ───────────────────────────────────────────────────

    [Fact]
    public void Update_ReplacesAllTranslationsAndModifiers()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);
        var originalCreatedAt = group.CreatedAt;

        var newTranslations = new (string, string)[]
        {
            ("nl", "Groottes"),
            ("fr", "Tailles"),
        };
        var newModifiers = new (decimal, int, IEnumerable<(string, string)>)[]
        {
            (0.00m, 0, [("nl", "Klein"), ("fr", "Petit")]),
            (1.00m, 1, [("nl", "Groot"), ("fr", "Grand")]),
        };

        group.Update(newTranslations, newModifiers);

        group.Translations.Count.ShouldBe(2);
        group.Translations.ShouldContain(t => t.LanguageCode == "nl" && t.Name == "Groottes");
        group.Translations.ShouldContain(t => t.LanguageCode == "fr" && t.Name == "Tailles");
        group.Modifiers.Count.ShouldBe(2);
        group.UpdatedAt.ShouldBeGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Fact]
    public void Update_WithNoTranslations_ThrowsArgumentException()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        Should.Throw<ArgumentException>(() =>
            group.Update(Array.Empty<(string, string)>(), ValidModifiers));
    }

    // ── ModifierGroup.SoftDelete ───────────────────────────────────────────────

    [Fact]
    public void SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);
        group.ClearDomainEvents();

        group.SoftDelete();

        group.IsDeleted.ShouldBeTrue();
        group.DeletedAt.ShouldNotBeNull();
        group.DomainEvents.Count.ShouldBe(1);
        group.DomainEvents.ShouldContain(e => e is ModifierGroupDeletedEvent);
    }

    [Fact]
    public void SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);
        group.ClearDomainEvents();

        group.SoftDelete();
        var firstDeletedAt = group.DeletedAt;
        group.ClearDomainEvents();

        group.SoftDelete(); // Second call

        group.IsDeleted.ShouldBeTrue();
        group.DeletedAt.ShouldBe(firstDeletedAt);
        group.DomainEvents.Count.ShouldBe(0); // No duplicate event
    }
}

public sealed class ModifierTests
{
    private static readonly (string languageCode, string name)[] ValidTranslations =
    [
        ("nl", "Mayonaise"),
    ];

    // ── Modifier ───────────────────────────────────────────────────────────────

    [Fact]
    public void Modifier_AllowsNegativePriceAdjustment()
    {
        var modifier = Modifier.Create(-0.50m, 0, ValidTranslations);

        modifier.ShouldNotBeNull();
        modifier.PriceAdjustment.ShouldBe(-0.50m);
    }

    [Fact]
    public void Modifier_AllowsZeroPriceAdjustment()
    {
        var modifier = Modifier.Create(0.00m, 0, ValidTranslations);

        modifier.ShouldNotBeNull();
        modifier.PriceAdjustment.ShouldBe(0.00m);
    }

    [Fact]
    public void Modifier_RequiresAtLeastOneTranslation()
    {
        Should.Throw<ArgumentException>(() =>
            Modifier.Create(0.00m, 0, Array.Empty<(string, string)>()));
    }
}

public sealed class ProductModifierGroupTests
{
    // ── ProductModifierGroup ───────────────────────────────────────────────────

    [Fact]
    public void ProductModifierGroup_Create_SetsAllProperties()
    {
        var productId = Guid.CreateVersion7();
        var modifierGroupId = Guid.CreateVersion7();
        const int sortOrder = 2;

        var pmg = ProductModifierGroup.Create(productId, modifierGroupId, sortOrder);

        pmg.ShouldNotBeNull();
        pmg.Id.ShouldNotBe(Guid.Empty);
        pmg.ProductId.ShouldBe(productId);
        pmg.ModifierGroupId.ShouldBe(modifierGroupId);
        pmg.SortOrder.ShouldBe(sortOrder);
    }
}
