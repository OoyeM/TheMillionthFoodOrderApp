using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Products;

public sealed class Product : AggregateRoot<Guid>, IAuditable, ISoftDeletable
{
    public Money BasePrice { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<ProductTranslation> _translations = [];
    public IReadOnlyCollection<ProductTranslation> Translations => _translations.AsReadOnly();

    // Required by EF Core
    private Product() { }

    /// <summary>
    /// Factory method — the only way to create a valid Product.
    /// Requires at least one translation.
    /// </summary>
    public static Product Create(
        Money basePrice,
        string? imageUrl,
        IEnumerable<(string languageCode, string name, string? description)> translations)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        var now = DateTimeOffset.UtcNow;
        var product = new Product
        {
            Id = Guid.CreateVersion7(),
            BasePrice = basePrice,
            ImageUrl = imageUrl,
            IsDeleted = false,
            CreatedAt = now,
            UpdatedAt = now,
        };

        foreach (var (languageCode, name, description) in translationList)
        {
            product._translations.Add(
                ProductTranslation.Create(product.Id, languageCode, name, description));
        }

        product.AddDomainEvent(new ProductCreatedEvent(product.Id));

        return product;
    }

    /// <summary>
    /// Updates product details. Replaces all translations (clear + re-add).
    /// </summary>
    public void Update(
        Money basePrice,
        string? imageUrl,
        IEnumerable<(string languageCode, string name, string? description)> translations)
    {
        var translationList = translations.ToList();
        if (translationList.Count == 0)
            throw new ArgumentException("At least one translation is required.", nameof(translations));

        BasePrice = basePrice;
        ImageUrl = imageUrl;
        UpdatedAt = DateTimeOffset.UtcNow;

        _translations.Clear();
        foreach (var (languageCode, name, description) in translationList)
        {
            _translations.Add(
                ProductTranslation.Create(Id, languageCode, name, description));
        }
    }

    /// <summary>
    /// Soft-deletes this product. Hidden from storefronts but retained for historical order records.
    /// </summary>
    public void SoftDelete()
    {
        if (IsDeleted)
            return;

        IsDeleted = true;
        DeletedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ProductDeletedEvent(Id));
    }
}
