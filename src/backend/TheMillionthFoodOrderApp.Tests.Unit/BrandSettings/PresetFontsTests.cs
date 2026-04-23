using Shouldly;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class PresetFontsTests
{
    // ── All collection ─────────────────────────────────────────────────────────

    [Fact]
    public void All_ContainsExactlyElevenFonts()
    {
        PresetFonts.All.Count.ShouldBe(11);
    }

    [Fact]
    public void All_IncludesSystemDefault()
    {
        PresetFonts.All.ShouldContain(PresetFonts.SystemDefault);
    }

    // ── IsValid — known fonts ─────────────────────────────────────────────────

    [Theory]
    [InlineData("System Default")]
    [InlineData("Inter")]
    [InlineData("Roboto")]
    [InlineData("Open Sans")]
    [InlineData("Lato")]
    [InlineData("Poppins")]
    [InlineData("Montserrat")]
    [InlineData("Nunito")]
    [InlineData("Raleway")]
    [InlineData("Source Sans 3")]
    [InlineData("DM Sans")]
    public void IsValid_WithRegisteredFont_ReturnsTrue(string font)
    {
        PresetFonts.IsValid(font).ShouldBeTrue();
    }

    // ── IsValid — invalid fonts ───────────────────────────────────────────────

    [Fact]
    public void IsValid_WithNullFont_ReturnsFalse()
    {
        PresetFonts.IsValid(null!).ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithEmptyString_ReturnsFalse()
    {
        PresetFonts.IsValid("").ShouldBeFalse();
    }

    [Fact]
    public void IsValid_WithUnknownFont_ReturnsFalse()
    {
        PresetFonts.IsValid("NotAFont").ShouldBeFalse();
    }

    // IsValid uses StringComparer.Ordinal — case-sensitive exact match
    [Theory]
    [InlineData("inter")]
    [InlineData("INTER")]
    [InlineData("ROBOTO")]
    public void IsValid_WithKnownFontInWrongCase_ReturnsFalse(string font)
    {
        PresetFonts.IsValid(font).ShouldBeFalse();
    }

    [Theory]
    [InlineData("Arial")]
    [InlineData("Comic Sans MS")]
    [InlineData("Times New Roman")]
    public void IsValid_WithCommonNonApprovedFont_ReturnsFalse(string font)
    {
        PresetFonts.IsValid(font).ShouldBeFalse();
    }
}
