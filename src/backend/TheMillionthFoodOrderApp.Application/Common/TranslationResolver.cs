namespace TheMillionthFoodOrderApp.Application.Common;

/// <summary>
/// Resolves a display name from a collection of translations using a fallback chain:
/// preferred language → first available → "(unnamed)".
/// </summary>
public static class TranslationResolver
{
    /// <summary>
    /// Resolves the best-match name from a translations collection.
    /// </summary>
    /// <param name="translations">The available translations.</param>
    /// <param name="languageCodeSelector">Extracts the language code from a translation.</param>
    /// <param name="nameSelector">Extracts the name from a translation.</param>
    /// <param name="primaryLanguage">The brand's default language (BCP-47 tag, e.g. "nl-BE").</param>
    public static string ResolveName<T>(
        IEnumerable<T> translations,
        Func<T, string> languageCodeSelector,
        Func<T, string> nameSelector,
        string primaryLanguage)
    {
        var primaryCode = ExtractLanguageCode(primaryLanguage);

        var primary = translations.FirstOrDefault(t => languageCodeSelector(t) == primaryCode);
        if (primary is not null)
            return nameSelector(primary);

        var first = translations.FirstOrDefault();
        return first is not null ? nameSelector(first) : "(unnamed)";
    }

    /// <summary>
    /// Extracts the two-letter language code from a BCP-47 tag (e.g. "nl-BE" → "nl").
    /// </summary>
    public static string ExtractLanguageCode(string bcp47Tag)
        => bcp47Tag.Split('-')[0].ToLowerInvariant();

    /// <summary>
    /// Validates that the brand's primary language is present in the translations collection.
    /// Throws <see cref="InvalidOperationException"/> if missing.
    /// </summary>
    public static void EnsurePrimaryLanguagePresent<T>(
        IEnumerable<T> translations,
        Func<T, string> languageCodeSelector,
        string primaryLanguage,
        string entityName)
    {
        var primaryCode = ExtractLanguageCode(primaryLanguage);

        if (!translations.Any(t => languageCodeSelector(t) == primaryCode))
            throw new InvalidOperationException(
                $"A translation in the brand's primary language ('{primaryCode}') is required for {entityName}.");
    }
}
