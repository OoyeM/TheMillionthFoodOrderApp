using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.BrandSettings;

/// <summary>
/// Brand-level configuration aggregate. Lives in the brand-specific database (brand_{slug}),
/// not in the shared platform database.
///
/// Each brand has exactly one BrandSettings record. The singleton row is created during
/// brand database provisioning and seeding.
/// </summary>
public sealed class BrandSettings : AggregateRoot<Guid>, IAuditable
{
    /// <summary>
    /// Default language code for this brand's storefronts (e.g. "nl-BE", "fr-BE").
    /// Must be a valid BCP-47 language tag.
    /// </summary>
    public string DefaultLanguage { get; private set; } = "nl-BE";

    /// <summary>
    /// IANA timezone identifier for this brand (e.g. "Europe/Brussels").
    /// Used when localising order timestamps and scheduling promotions.
    /// </summary>
    public string Timezone { get; private set; } = "Europe/Brussels";

    /// <summary>
    /// ISO 4217 currency code (e.g. "EUR").
    /// </summary>
    public string Currency { get; private set; } = "EUR";

    // ── Theming ──────────────────────────────────────────────────────────────

    /// <summary>
    /// URL of the brand logo image. Null when no logo has been uploaded.
    /// The storefront falls back to displaying the brand name as text.
    /// </summary>
    public string? LogoUrl { get; private set; }

    /// <summary>
    /// Custom domain for this brand's storefront (e.g. "order.frietjes.be").
    /// Stored only — actual DNS routing is handled by US-FP-067.
    /// </summary>
    public string? CustomDomain { get; private set; }

    /// <summary>
    /// Brand color palette. Null until the brand admin has configured theming;
    /// the storefront applies sensible defaults when null.
    /// </summary>
    public BrandColors? Colors { get; private set; }

    /// <summary>
    /// Brand typography settings. Null until the brand admin has configured theming.
    /// </summary>
    public BrandTypography? Typography { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset CreatedAt { get; private set; }

    /// <inheritdoc />
    public DateTimeOffset UpdatedAt { get; private set; }

    // Required by EF Core
    private BrandSettings() { }

    /// <summary>
    /// Factory method — creates a new BrandSettings aggregate with Belgian defaults.
    /// </summary>
    public static BrandSettings CreateDefault()
    {
        var now = DateTimeOffset.UtcNow;
        return new BrandSettings
        {
            Id = Guid.CreateVersion7(),
            DefaultLanguage = "nl-BE",
            Timezone = "Europe/Brussels",
            Currency = "EUR",
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Factory method — creates BrandSettings with explicit values.
    /// </summary>
    public static BrandSettings Create(string defaultLanguage, string timezone, string currency)
    {
        var now = DateTimeOffset.UtcNow;
        return new BrandSettings
        {
            Id = Guid.CreateVersion7(),
            DefaultLanguage = defaultLanguage,
            Timezone = timezone,
            Currency = currency,
            CreatedAt = now,
            UpdatedAt = now,
        };
    }

    /// <summary>
    /// Updates the brand settings with new values.
    /// </summary>
    public void Update(string defaultLanguage, string timezone, string currency)
    {
        DefaultLanguage = defaultLanguage;
        Timezone = timezone;
        Currency = currency;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Updates the visual theming configuration: colors, typography, and custom domain.
    /// Pass <c>null</c> for any parameter to clear that theming property.
    /// </summary>
    public void UpdateTheming(
        BrandColors? colors,
        BrandTypography? typography,
        string? customDomain)
    {
        Colors = colors;
        Typography = typography;
        CustomDomain = customDomain;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Sets (or clears) the brand logo URL after a successful upload.
    /// </summary>
    public void SetLogoUrl(string? logoUrl)
    {
        LogoUrl = logoUrl;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
