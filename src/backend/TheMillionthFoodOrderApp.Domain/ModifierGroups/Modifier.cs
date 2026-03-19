using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

/// <summary>
/// A selectable option within a modifier group (e.g. "Small", "Mayonnaise").
/// PriceAdjustment is a plain decimal — can be negative (discount) or zero.
/// </summary>
public sealed class Modifier : Entity<Guid>
{
    /// <summary>
    /// Price delta applied when this modifier is selected. Can be negative.
    /// Currency is implicitly EUR (same as the brand's default).
    /// </summary>
    public decimal PriceAdjustment { get; private set; }

    /// <summary>
    /// Display position of this modifier within its group. 0-based, ascending.
    /// </summary>
    public int SortOrder { get; private set; }

    private readonly List<ModifierTranslation> _translations = [];
    public IReadOnlyCollection<ModifierTranslation> Translations => _translations.AsReadOnly();

    private Modifier() { } // EF Core

    public static Modifier Create(
        decimal priceAdjustment,
        int sortOrder,
        IEnumerable<(string languageCode, string name)> translations)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var modifier = new Modifier
        {
            Id = Guid.CreateVersion7(),
            PriceAdjustment = priceAdjustment,
            SortOrder = sortOrder,
        };

        foreach (var (languageCode, name) in translationList)
        {
            modifier._translations.Add(ModifierTranslation.Create(modifier.Id, languageCode, name));
        }

        return modifier;
    }
}
