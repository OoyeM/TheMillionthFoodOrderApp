using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class PresetFontsTests
{
    // ── All collection ─────────────────────────────────────────────────────────

    [Test]
    public async Task All_ContainsExactlyElevenFonts()
    {
        await Assert.That(PresetFonts.All.Count).IsEqualTo(11);
    }

    [Test]
    public async Task All_IncludesSystemDefault()
    {
        await Assert.That(PresetFonts.All).Contains(PresetFonts.SystemDefault);
    }

    // ── IsValid — known fonts ─────────────────────────────────────────────────

    [Arguments("System Default")]
    [Arguments("Inter")]
    [Arguments("Roboto")]
    [Arguments("Open Sans")]
    [Arguments("Lato")]
    [Arguments("Poppins")]
    [Arguments("Montserrat")]
    [Arguments("Nunito")]
    [Arguments("Raleway")]
    [Arguments("Source Sans 3")]
    [Arguments("DM Sans")]
    [Test]
    public async Task IsValid_WithRegisteredFont_ReturnsTrue(string font)
    {
        await Assert.That(PresetFonts.IsValid(font)).IsTrue();
    }

    // ── IsValid — invalid fonts ───────────────────────────────────────────────

    [Test]
    public async Task IsValid_WithNullFont_ReturnsFalse()
    {
        await Assert.That(PresetFonts.IsValid(null!)).IsFalse();
    }

    [Test]
    public async Task IsValid_WithEmptyString_ReturnsFalse()
    {
        await Assert.That(PresetFonts.IsValid("")).IsFalse();
    }

    [Test]
    public async Task IsValid_WithUnknownFont_ReturnsFalse()
    {
        await Assert.That(PresetFonts.IsValid("NotAFont")).IsFalse();
    }

    // IsValid uses StringComparer.Ordinal — case-sensitive exact match
    [Arguments("inter")]
    [Arguments("INTER")]
    [Arguments("ROBOTO")]
    [Test]
    public async Task IsValid_WithKnownFontInWrongCase_ReturnsFalse(string font)
    {
        await Assert.That(PresetFonts.IsValid(font)).IsFalse();
    }

    [Arguments("Arial")]
    [Arguments("Comic Sans MS")]
    [Arguments("Times New Roman")]
    [Test]
    public async Task IsValid_WithCommonNonApprovedFont_ReturnsFalse(string font)
    {
        await Assert.That(PresetFonts.IsValid(font)).IsFalse();
    }
}
