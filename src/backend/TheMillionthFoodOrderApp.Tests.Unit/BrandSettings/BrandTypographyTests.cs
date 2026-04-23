using Shouldly;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class BrandTypographyTests
{
    // ── Constructor — happy path ───────────────────────────────────────────────

    [Theory]
    [MemberData(nameof(AllPresetFontPairs))]
    public void Constructor_WithPresetFonts_SetsPropertiesCorrectly(string heading, string body)
    {
        var typography = new BrandTypography(heading, body);

        typography.HeadingFontFamily.ShouldBe(heading);
        typography.BodyFontFamily.ShouldBe(body);
    }

    public static IEnumerable<object[]> AllPresetFontPairs()
    {
        // Use SystemDefault paired with each preset font to cover all approved fonts
        foreach (var font in PresetFonts.All)
            yield return [font, PresetFonts.SystemDefault];
    }

    [Fact]
    public void Constructor_WithSameFontForBothRoles_SetsPropertiesCorrectly()
    {
        var typography = new BrandTypography(PresetFonts.SystemDefault, PresetFonts.SystemDefault);

        typography.HeadingFontFamily.ShouldBe(PresetFonts.SystemDefault);
        typography.BodyFontFamily.ShouldBe(PresetFonts.SystemDefault);
    }

    // ── Constructor — invalid fonts ───────────────────────────────────────────

    [Fact]
    public void Constructor_WithUnknownHeadingFont_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandTypography("Comic Sans MS", PresetFonts.SystemDefault));
    }

    [Fact]
    public void Constructor_WithUnknownBodyFont_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandTypography(PresetFonts.SystemDefault, "Arial"));
    }

    [Fact]
    public void Constructor_WithEmptyHeadingFont_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandTypography("", PresetFonts.SystemDefault));
    }

    [Fact]
    public void Constructor_WithEmptyBodyFont_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandTypography(PresetFonts.SystemDefault, ""));
    }

    // ── Equality ───────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_WithIdenticalValues_AreEqual()
    {
        var a = new BrandTypography("Inter", "Roboto");
        var b = new BrandTypography("Inter", "Roboto");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_WithDifferentValues_AreNotEqual()
    {
        var a = new BrandTypography("Inter", "Roboto");
        var b = new BrandTypography("Poppins", "Roboto");

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }
}
