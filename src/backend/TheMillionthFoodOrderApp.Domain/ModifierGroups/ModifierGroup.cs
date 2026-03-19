using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

/// <summary>
/// A named group of selectable modifiers (e.g. "Size", "Sauces").
/// Modifier groups are brand-scoped and can be shared across multiple products.
/// </summary>
public sealed class ModifierGroup : AggregateRoot<Guid>, IAuditable, ISoftDeletable
{
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<ModifierGroupTranslation> _translations = [];
    public IReadOnlyCollection<ModifierGroupTranslation> Translations => _translations.AsReadOnly();

    private readonly List<Modifier> _modifiers = [];
    public IReadOnlyCollection<Modifier> Modifiers => _modifiers.AsReadOnly();

    // Required by EF Core
    private ModifierGroup() { }

    /// <summary>
    /// Factory method — the only way to create a valid ModifierGroup.
    /// Requires at least one translation and at least one modifier.
    /// </summary>
    public static ModifierGroup Create(
        IEnumerable<(string languageCode, string name)> translations,
        IEnumerable<(decimal priceAdjustment, int sortOrder, IEnumerable<(string languageCode, string name)> translations)> modifiers)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var modifierList = modifiers.ToList();
        if (modifierList.Count == 0)
            throw new ArgumentException("At least one modifier is required.", nameof(modifiers));

        var now = DateTimeOffset.UtcNow;
        var group = new ModifierGroup
        {
            Id = Guid.CreateVersion7(),
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (languageCode, name) in translationList)
        {
            group._translations.Add(ModifierGroupTranslation.Create(group.Id, languageCode, name));
        }

        foreach (var (priceAdjustment, sortOrder, modifierTranslations) in modifierList)
        {
            group._modifiers.Add(Modifier.Create(priceAdjustment, sortOrder, modifierTranslations));
        }

        group.AddDomainEvent(new ModifierGroupCreatedEvent(group.Id));

        return group;
    }

    /// <summary>
    /// Updates the modifier group. Replaces all translations and modifiers (clear + re-add).
    /// </summary>
    public void Update(
        IEnumerable<(string languageCode, string name)> translations,
        IEnumerable<(decimal priceAdjustment, int sortOrder, IEnumerable<(string languageCode, string name)> translations)> modifiers)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var modifierList = modifiers.ToList();
        if (modifierList.Count == 0)
            throw new ArgumentException("At least one modifier is required.", nameof(modifiers));

        UpdatedAt = DateTimeOffset.UtcNow;

        _translations.Clear();
        foreach (var (languageCode, name) in translationList)
        {
            _translations.Add(ModifierGroupTranslation.Create(Id, languageCode, name));
        }

        _modifiers.Clear();
        foreach (var (priceAdjustment, sortOrder, modifierTranslations) in modifierList)
        {
            _modifiers.Add(Modifier.Create(priceAdjustment, sortOrder, modifierTranslations));
        }
    }

    /// <summary>
    /// Soft-deletes this modifier group. Hidden from storefronts but retained for historical order records.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ModifierGroupDeletedEvent(Id));
    }
}
