using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.BrandSettings;

/// <summary>
/// Value object representing the font families used for brand theming.
/// Both font families must be selected from <see cref="PresetFonts.All"/>.
/// </summary>
public sealed class BrandTypography : ValueObject
{
    /// <summary>Font family applied to headings (h1–h6).</summary>
    public string HeadingFontFamily { get; }

    /// <summary>Font family applied to body text and UI elements.</summary>
    public string BodyFontFamily { get; }

    // Required by EF Core owned-entity materialisation
    private BrandTypography() { HeadingFontFamily = PresetFonts.SystemDefault; BodyFontFamily = PresetFonts.SystemDefault; }

    /// <summary>
    /// Creates a <see cref="BrandTypography"/> instance after validating both font families against
    /// the <see cref="PresetFonts"/> approved list.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when either font family is not in the approved list.</exception>
    public BrandTypography(string headingFontFamily, string bodyFontFamily)
    {
        if (!PresetFonts.IsValid(headingFontFamily))
            throw new ArgumentException(
                $"'{headingFontFamily}' is not an approved font family. Choose from: {string.Join(", ", PresetFonts.All)}.",
                nameof(headingFontFamily));

        if (!PresetFonts.IsValid(bodyFontFamily))
            throw new ArgumentException(
                $"'{bodyFontFamily}' is not an approved font family. Choose from: {string.Join(", ", PresetFonts.All)}.",
                nameof(bodyFontFamily));

        HeadingFontFamily = headingFontFamily;
        BodyFontFamily = bodyFontFamily;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return HeadingFontFamily;
        yield return BodyFontFamily;
    }
}
