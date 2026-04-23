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

    [Test]
    public async Task Create_WithValidData_ReturnsModifierGroup()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        await Assert.That(group).IsNotNull();
        await Assert.That(group.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(group.IsDeleted).IsFalse();
        await Assert.That(group.DeletedAt).IsNull();
        await Assert.That(group.Translations.Count).IsEqualTo(1);
        await Assert.That(group.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Sauzen");
        await Assert.That(group.Modifiers.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Create_WithMultipleModifiers_IncludesAll()
    {
        var modifiers = new (decimal, int, IEnumerable<(string, string)>)[]
        {
            (0.00m, 0, [("nl", "Klein")]),
            (0.50m, 1, [("nl", "Middel")]),
            (1.00m, 2, [("nl", "Groot")]),
        };

        var group = ModifierGroup.Create(ValidTranslations, modifiers);

        await Assert.That(group.Modifiers.Count).IsEqualTo(3);
    }

    [Test]
    public async Task Create_WithNoTranslations_ThrowsArgumentException()
    {
        var emptyTranslations = Array.Empty<(string, string)>();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(ModifierGroup.Create(emptyTranslations, ValidModifiers)));
    }

    [Test]
    public async Task Create_RaisesModifierGroupCreatedEvent()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        await Assert.That(group.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(group.DomainEvents).Contains(e => e is ModifierGroupCreatedEvent);
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (group.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    [Test]
    public async Task Create_AssignsSequentialSortOrderToModifiers()
    {
        var modifiers = new (decimal, int, IEnumerable<(string, string)>)[]
        {
            (0.00m, 0, [("nl", "Klein")]),
            (0.50m, 1, [("nl", "Middel")]),
            (1.00m, 2, [("nl", "Groot")]),
        };

        var group = ModifierGroup.Create(ValidTranslations, modifiers);

        var sortOrders = group.Modifiers.Select(m => m.SortOrder).OrderBy(s => s).ToList();
        await Assert.That(sortOrders).IsEquivalentTo([0, 1, 2]);
    }

    // ── ModifierGroup.Update ───────────────────────────────────────────────────

    [Test]
    public async Task Update_ReplacesAllTranslationsAndModifiers()
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

        await Assert.That(group.Translations.Count).IsEqualTo(2);
        await Assert.That(group.Translations).Contains(t => t.LanguageCode == "nl" && t.Name == "Groottes");
        await Assert.That(group.Translations).Contains(t => t.LanguageCode == "fr" && t.Name == "Tailles");
        await Assert.That(group.Modifiers.Count).IsEqualTo(2);
        await Assert.That(group.UpdatedAt).IsGreaterThanOrEqualTo(originalCreatedAt);
    }

    [Test]
    public async Task Update_WithNoTranslations_ThrowsArgumentException()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            group.Update(Array.Empty<(string, string)>(), ValidModifiers);
            return Task.CompletedTask;
        });
    }

    // ── ModifierGroup.SoftDelete ───────────────────────────────────────────────

    [Test]
    public async Task SoftDelete_SetsIsDeletedAndDeletedAt()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);
        group.ClearDomainEvents();

        group.SoftDelete();

        await Assert.That(group.IsDeleted).IsTrue();
        await Assert.That(group.DeletedAt).IsNotNull();
        await Assert.That(group.DomainEvents.Count).IsEqualTo(1);
        await Assert.That(group.DomainEvents).Contains(e => e is ModifierGroupDeletedEvent);
    }

    [Test]
    public async Task SoftDelete_WhenAlreadyDeleted_IsIdempotent()
    {
        var group = ModifierGroup.Create(ValidTranslations, ValidModifiers);
        group.ClearDomainEvents();

        group.SoftDelete();
        var firstDeletedAt = group.DeletedAt;
        group.ClearDomainEvents();

        group.SoftDelete(); // Second call

        await Assert.That(group.IsDeleted).IsTrue();
        await Assert.That(group.DeletedAt).IsEqualTo(firstDeletedAt);
        await Assert.That(group.DomainEvents.Count).IsEqualTo(0); // No duplicate event
    }
}

public sealed class ModifierTests
{
    private static readonly (string languageCode, string name)[] ValidTranslations =
    [
        ("nl", "Mayonaise"),
    ];

    // ── Modifier ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Modifier_AllowsNegativePriceAdjustment()
    {
        var modifier = Modifier.Create(-0.50m, 0, ValidTranslations);

        await Assert.That(modifier).IsNotNull();
        await Assert.That(modifier.PriceAdjustment).IsEqualTo(-0.50m);
    }

    [Test]
    public async Task Modifier_AllowsZeroPriceAdjustment()
    {
        var modifier = Modifier.Create(0.00m, 0, ValidTranslations);

        await Assert.That(modifier).IsNotNull();
        await Assert.That(modifier.PriceAdjustment).IsEqualTo(0.00m);
    }

    [Test]
    public async Task Modifier_RequiresAtLeastOneTranslation()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(Modifier.Create(0.00m, 0, Array.Empty<(string, string)>())));
    }
}

public sealed class ProductModifierGroupTests
{
    // ── ProductModifierGroup ───────────────────────────────────────────────────

    [Test]
    public async Task ProductModifierGroup_Create_SetsAllProperties()
    {
        var productId = Guid.CreateVersion7();
        var modifierGroupId = Guid.CreateVersion7();
        const int sortOrder = 2;

        var pmg = ProductModifierGroup.Create(productId, modifierGroupId, sortOrder);

        await Assert.That(pmg).IsNotNull();
        await Assert.That(pmg.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(pmg.ProductId).IsEqualTo(productId);
        await Assert.That(pmg.ModifierGroupId).IsEqualTo(modifierGroupId);
        await Assert.That(pmg.SortOrder).IsEqualTo(sortOrder);
    }
}
