using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.BrandSettings;

/// <summary>
/// Value object representing the three brand colors used for storefront theming.
/// Each color must be a valid CSS hex color string (e.g. "#2563eb" or "#fff").
/// </summary>
public sealed class BrandColors : ValueObject
{
    /// <summary>Primary brand color — used for main UI elements (buttons, headings).</summary>
    public string Primary { get; }

    /// <summary>Secondary brand color — used for supporting UI elements.</summary>
    public string Secondary { get; }

    /// <summary>Accent brand color — used for highlights, links, and call-to-action elements.</summary>
    public string Accent { get; }

    // Required by EF Core owned-entity materialisation
    private BrandColors() { Primary = string.Empty; Secondary = string.Empty; Accent = string.Empty; }

    /// <summary>
    /// Creates a <see cref="BrandColors"/> instance after validating all three hex color values.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when any color value is not a valid CSS hex color.</exception>
    public BrandColors(string primary, string secondary, string accent)
    {
        if (!IsValidHex(primary))
            throw new ArgumentException($"'{primary}' is not a valid CSS hex color.", nameof(primary));
        if (!IsValidHex(secondary))
            throw new ArgumentException($"'{secondary}' is not a valid CSS hex color.", nameof(secondary));
        if (!IsValidHex(accent))
            throw new ArgumentException($"'{accent}' is not a valid CSS hex color.", nameof(accent));

        Primary = primary.ToLowerInvariant();
        Secondary = secondary.ToLowerInvariant();
        Accent = accent.ToLowerInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Primary;
        yield return Secondary;
        yield return Accent;
    }

    // ── Private helpers ──────────────────────────────────────────────────────

    /// <summary>
    /// Validates CSS hex color strings. Accepts 3-digit (#rgb) and 6-digit (#rrggbb) formats.
    /// </summary>
    private static bool IsValidHex(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value[0] != '#')
            return false;

        var hex = value[1..];
        return (hex.Length == 3 || hex.Length == 6) &&
               hex.All(Uri.IsHexDigit);
    }
}
