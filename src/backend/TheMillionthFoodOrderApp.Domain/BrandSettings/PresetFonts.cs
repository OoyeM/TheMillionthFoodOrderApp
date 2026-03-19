namespace TheMillionthFoodOrderApp.Domain.BrandSettings;

/// <summary>
/// Curated list of approved font families for brand theming.
/// Custom font uploads are deferred to a future user story.
/// All entries are either web-safe or available via Google Fonts.
/// </summary>
public static class PresetFonts
{
    /// <summary>
    /// Sentinel value meaning "use the browser / OS default font stack".
    /// No Google Fonts request is made when this value is selected.
    /// </summary>
    public const string SystemDefault = "System Default";

    /// <summary>All approved font family names, including the system default sentinel.</summary>
    public static readonly IReadOnlyList<string> All = new[]
    {
        SystemDefault,
        "Inter",
        "Roboto",
        "Open Sans",
        "Lato",
        "Poppins",
        "Montserrat",
        "Nunito",
        "Raleway",
        "Source Sans 3",
        "DM Sans",
    };

    /// <summary>Returns <c>true</c> when <paramref name="fontFamily"/> is in the approved list.</summary>
    public static bool IsValid(string fontFamily) =>
        All.Contains(fontFamily, StringComparer.Ordinal);
}
