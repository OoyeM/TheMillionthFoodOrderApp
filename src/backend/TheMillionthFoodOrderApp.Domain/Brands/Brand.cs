using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Brands;

public sealed class Brand : AggregateRoot<Guid>, IAuditable
{
    public string Name { get; private set; } = string.Empty;
    public string Slug { get; private set; } = string.Empty;
    public string ContactEmail { get; private set; } = string.Empty;
    public string? ContactPhone { get; private set; }
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Auto-generated database name for this brand's dedicated database.
    /// Actual provisioning is a future concern — stored here for reference.
    /// </summary>
    public string DatabaseName { get; private set; } = string.Empty;

    /// <summary>
    /// The authentication method allowed for staff members on this brand's management portal.
    /// Defaults to <see cref="StaffAuthMethod.EmailPassword"/>.
    /// </summary>
    public StaffAuthMethod StaffAuthMethod { get; private set; } = StaffAuthMethod.EmailPassword;

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core
    private Brand() { }

    /// <summary>
    /// Factory method — the only way to create a valid Brand.
    /// Raises <see cref="BrandCreatedEvent"/> on success.
    /// </summary>
    public static Brand Create(string name, string slug, string contactEmail, string? contactPhone)
    {
        var now = DateTimeOffset.UtcNow;
        var brand = new Brand
        {
            Id = Guid.CreateVersion7(),
            Name = name,
            Slug = slug,
            ContactEmail = contactEmail,
            ContactPhone = contactPhone,
            IsActive = true,
            DatabaseName = $"brand_{slug}",
            CreatedAt = now,
            UpdatedAt = now,
        };

        brand.AddDomainEvent(new BrandCreatedEvent(brand.Id, brand.Name, brand.Slug));

        return brand;
    }

    /// <summary>Updates mutable metadata. Slug is intentionally immutable after creation.</summary>
    public void UpdateMetadata(string name, string contactEmail, string? contactPhone)
    {
        Name = name;
        ContactEmail = contactEmail;
        ContactPhone = contactPhone;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Deactivates this brand, disabling all its shops and storefronts.
    /// Raises <see cref="BrandDeactivatedEvent"/>.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new BrandDeactivatedEvent(Id, Slug));
    }

    /// <summary>Re-activates a previously deactivated brand.</summary>
    public void Activate()
    {
        if (IsActive)
            return;

        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Configures the staff authentication method for this brand's management portal.
    /// Changes take effect on the next Entra app registration configuration sync.
    /// </summary>
    public void ConfigureStaffAuth(StaffAuthMethod method)
    {
        StaffAuthMethod = method;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
