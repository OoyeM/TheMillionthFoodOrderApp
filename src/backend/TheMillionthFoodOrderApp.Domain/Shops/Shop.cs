using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

public sealed class Shop : AggregateRoot<Guid>, IAuditable
{
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// URL-safe identifier, unique within the brand's database.
    /// Customer-facing URL pattern: /{brandSlug}/{lang}/shops/{shopSlug}.
    /// Immutable after creation.
    /// </summary>
    public string Slug { get; private set; } = string.Empty;

    public Address Address { get; private set; } = null!;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core
    private Shop() { }

    /// <summary>
    /// Factory method — the only way to create a valid Shop.
    /// Raises <see cref="ShopCreatedEvent"/> so a future handler can clone the brand's product catalog.
    /// </summary>
    public static Shop Create(
        string name,
        string slug,
        Address address,
        string contactEmail,
        string? contactPhone)
    {
        var now = DateTimeOffset.UtcNow;
        var shop = new Shop
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug,
            Address = address,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        shop.AddDomainEvent(new ShopCreatedEvent(shop.Id, shop.Name, shop.Slug));

        return shop;
    }

    /// <summary>Updates mutable metadata. Slug is intentionally immutable after creation.</summary>
    public void UpdateMetadata(
        string name,
        Address address,
        string contactEmail,
        string? contactPhone)
    {
        Name = name;
        Address = address;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deactivates this shop, hiding it from customers.
    /// Raises <see cref="ShopDeactivatedEvent"/>.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new ShopDeactivatedEvent(Id, Slug));
    }

    /// <summary>Re-activates a previously deactivated shop.</summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
