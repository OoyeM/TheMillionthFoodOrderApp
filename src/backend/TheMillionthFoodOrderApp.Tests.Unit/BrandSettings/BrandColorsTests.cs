using Shouldly;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class BrandColorsTests
{
    // ── Constructor — happy path ───────────────────────────────────────────────

    [Theory]
    [InlineData("#fff", "#000", "#abc")]
    [InlineData("#ffffff", "#000000", "#abcdef")]
    [InlineData("#2563eb", "#64748b", "#f59e0b")]
    [InlineData("#abc", "#123456", "#aabbcc")]
    public void Constructor_WithValidHexColors_SetsProperties(string primary, string secondary, string accent)
    {
        var colors = new BrandColors(primary, secondary, accent);

        colors.Primary.ShouldBe(primary.ToLowerInvariant());
        colors.Secondary.ShouldBe(secondary.ToLowerInvariant());
        colors.Accent.ShouldBe(accent.ToLowerInvariant());
    }

    [Theory]
    [InlineData("#ABC", "#abc")]
    [InlineData("#FFAA00", "#ffaa00")]
    [InlineData("#2563EB", "#2563eb")]
    public void Constructor_LowercasesHexOnStore(string input, string expected)
    {
        var colors = new BrandColors(input, "#000000", "#000000");

        colors.Primary.ShouldBe(expected);
    }

    // ── Constructor — invalid colors ───────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyOrWhitespacePrimary_ThrowsArgumentException(string badValue)
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors(badValue, "#000000", "#000000"));
    }

    [Fact]
    public void Constructor_WithEmptySecondary_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors("#ffffff", "", "#000000"));
    }

    [Fact]
    public void Constructor_WithEmptyAccent_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors("#ffffff", "#000000", ""));
    }

    [Theory]
    [InlineData("ffffff")]
    [InlineData("rgb(0,0,0)")]
    [InlineData("blue")]
    public void Constructor_WithMissingHashPrefix_ThrowsArgumentException(string badValue)
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors(badValue, "#000000", "#000000"));
    }

    [Theory]
    [InlineData("#gggggg")]
    [InlineData("#zzzzzz")]
    [InlineData("#12345g")]
    public void Constructor_WithNonHexCharacters_ThrowsArgumentException(string badValue)
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors(badValue, "#000000", "#000000"));
    }

    [Theory]
    [InlineData("#ffaa")]
    [InlineData("#ffaa0")]
    [InlineData("#ffaa000")]
    [InlineData("#ff")]
    [InlineData("#f")]
    [InlineData("#")]
    public void Constructor_WithWrongHexLength_ThrowsArgumentException(string badValue)
    {
        Should.Throw<ArgumentException>(() =>
            new BrandColors(badValue, "#000000", "#000000"));
    }

    // ── Equality ───────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_WithIdenticalNormalizedValues_AreEqual()
    {
        var a = new BrandColors("#FFFFFF", "#000000", "#ABCDEF");
        var b = new BrandColors("#ffffff", "#000000", "#abcdef");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_WithDifferentValues_AreNotEqual()
    {
        var a = new BrandColors("#ffffff", "#000000", "#abcdef");
        var b = new BrandColors("#ffffff", "#111111", "#abcdef");

        a.ShouldNotBe(b);
        (a == b).ShouldBeFalse();
    }
}
