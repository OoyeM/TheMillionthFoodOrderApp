using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.MenuCategories;

public sealed class MenuCategory : AggregateRoot<Guid>, IAuditable, ISoftDeletable
{
    public string? ImageUrl { get; private set; }

    /// <summary>
    /// Display order for menu rendering. Lower values appear first.
    /// </summary>
    public int SortOrder { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<MenuCategoryTranslation> _translations = [];
    public IReadOnlyCollection<MenuCategoryTranslation> Translations => _translations.AsReadOnly();

    // Required by EF Core
    private MenuCategory() { }

    /// <summary>
    /// Factory method — the only way to create a valid MenuCategory.
    /// Requires at least one translation.
    /// </summary>
    public static MenuCategory Create(
        string? imageUrl,
        int sortOrder,
        IEnumerable<(string languageCode, string name, string? description)> translations)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var now = DateTimeOffset.UtcNow;
        var category = new MenuCategory
        {
            Id = Guid.CreateVersion7(),
            ImageUrl = imageUrl,
            SortOrder = sortOrder,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (languageCode, name, description) in translationList)
        {
            category._translations.Add(
                MenuCategoryTranslation.Create(category.Id, languageCode, name, description));
        }

        category.AddDomainEvent(new MenuCategoryCreatedEvent(category.Id));

        return category;
    }

    /// <summary>
    /// Updates category details. Replaces all translations (clear + re-add).
    /// </summary>
    public void Update(
        string? imageUrl,
        int sortOrder,
        IEnumerable<(string languageCode, string name, string? description)> translations)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        ImageUrl = imageUrl;
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;

        _translations.Clear();
        foreach (var (languageCode, name, description) in translationList)
        {
            _translations.Add(
                MenuCategoryTranslation.Create(Id, languageCode, name, description));
        }
    }

    /// <summary>
    /// Updates the sort order without touching translations. Used by the reorder endpoint.
    /// </summary>
    public void Reorder(int sortOrder)
    {
        SortOrder = sortOrder;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Soft-deletes this category. Hidden from storefronts but retained for historical data.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new MenuCategoryDeletedEvent(Id));
    }
}
