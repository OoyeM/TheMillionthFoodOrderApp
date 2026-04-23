using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class BrandTypographyTests
{
    // ── Constructor — happy path ───────────────────────────────────────────────

    [MethodDataSource(nameof(AllPresetFontPairs))]
    [Test]
    public async Task Constructor_WithPresetFonts_SetsPropertiesCorrectly(string heading, string body)
    {
        var typography = new BrandTypography(heading, body);

        await Assert.That(typography.HeadingFontFamily).IsEqualTo(heading);
        await Assert.That(typography.BodyFontFamily).IsEqualTo(body);
    }

    public static IEnumerable<(string, string)> AllPresetFontPairs()
    {
        // Use SystemDefault paired with each preset font to cover all approved fonts
        foreach (var font in PresetFonts.All)
            yield return (font, PresetFonts.SystemDefault);
    }

    [Test]
    public async Task Constructor_WithSameFontForBothRoles_SetsPropertiesCorrectly()
    {
        var typography = new BrandTypography(PresetFonts.SystemDefault, PresetFonts.SystemDefault);

        await Assert.That(typography.HeadingFontFamily).IsEqualTo(PresetFonts.SystemDefault);
        await Assert.That(typography.BodyFontFamily).IsEqualTo(PresetFonts.SystemDefault);
    }

    // ── Constructor — invalid fonts ───────────────────────────────────────────

    [Test]
    public async Task Constructor_WithUnknownHeadingFont_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandTypography("Comic Sans MS", PresetFonts.SystemDefault)));
    }

    [Test]
    public async Task Constructor_WithUnknownBodyFont_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandTypography(PresetFonts.SystemDefault, "Arial")));
    }

    [Test]
    public async Task Constructor_WithEmptyHeadingFont_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandTypography("", PresetFonts.SystemDefault)));
    }

    [Test]
    public async Task Constructor_WithEmptyBodyFont_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandTypography(PresetFonts.SystemDefault, "")));
    }

    // ── Equality ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Equality_WithIdenticalValues_AreEqual()
    {
        var a = new BrandTypography("Inter", "Roboto");
        var b = new BrandTypography("Inter", "Roboto");

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task Equality_WithDifferentValues_AreNotEqual()
    {
        var a = new BrandTypography("Inter", "Roboto");
        var b = new BrandTypography("Poppins", "Roboto");

        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(a == b).IsFalse();
    }
}
