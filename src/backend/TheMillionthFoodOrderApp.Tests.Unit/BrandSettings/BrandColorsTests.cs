using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Tests.Unit.BrandSettings;

public sealed class BrandColorsTests
{
    // ── Constructor — happy path ───────────────────────────────────────────────

    [Arguments("#fff", "#000", "#abc")]
    [Arguments("#ffffff", "#000000", "#abcdef")]
    [Arguments("#2563eb", "#64748b", "#f59e0b")]
    [Arguments("#abc", "#123456", "#aabbcc")]
    [Test]
    public async Task Constructor_WithValidHexColors_SetsProperties(string primary, string secondary, string accent)
    {
        var colors = new BrandColors(primary, secondary, accent);

        await Assert.That(colors.Primary).IsEqualTo(primary.ToLowerInvariant());
        await Assert.That(colors.Secondary).IsEqualTo(secondary.ToLowerInvariant());
        await Assert.That(colors.Accent).IsEqualTo(accent.ToLowerInvariant());
    }

    [Arguments("#ABC", "#abc")]
    [Arguments("#FFAA00", "#ffaa00")]
    [Arguments("#2563EB", "#2563eb")]
    [Test]
    public async Task Constructor_LowercasesHexOnStore(string input, string expected)
    {
        var colors = new BrandColors(input, "#000000", "#000000");

        await Assert.That(colors.Primary).IsEqualTo(expected);
    }

    // ── Constructor — invalid colors ───────────────────────────────────────────

    [Arguments("")]
    [Arguments("   ")]
    [Test]
    public async Task Constructor_WithEmptyOrWhitespacePrimary_ThrowsArgumentException(string badValue)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors(badValue, "#000000", "#000000")));
    }

    [Test]
    public async Task Constructor_WithEmptySecondary_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors("#ffffff", "", "#000000")));
    }

    [Test]
    public async Task Constructor_WithEmptyAccent_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors("#ffffff", "#000000", "")));
    }

    [Arguments("ffffff")]
    [Arguments("rgb(0,0,0)")]
    [Arguments("blue")]
    [Test]
    public async Task Constructor_WithMissingHashPrefix_ThrowsArgumentException(string badValue)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors(badValue, "#000000", "#000000")));
    }

    [Arguments("#gggggg")]
    [Arguments("#zzzzzz")]
    [Arguments("#12345g")]
    [Test]
    public async Task Constructor_WithNonHexCharacters_ThrowsArgumentException(string badValue)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors(badValue, "#000000", "#000000")));
    }

    [Arguments("#ffaa")]
    [Arguments("#ffaa0")]
    [Arguments("#ffaa000")]
    [Arguments("#ff")]
    [Arguments("#f")]
    [Arguments("#")]
    [Test]
    public async Task Constructor_WithWrongHexLength_ThrowsArgumentException(string badValue)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new BrandColors(badValue, "#000000", "#000000")));
    }

    // ── Equality ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Equality_WithIdenticalNormalizedValues_AreEqual()
    {
        var a = new BrandColors("#FFFFFF", "#000000", "#ABCDEF");
        var b = new BrandColors("#ffffff", "#000000", "#abcdef");

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task Equality_WithDifferentValues_AreNotEqual()
    {
        var a = new BrandColors("#ffffff", "#000000", "#abcdef");
        var b = new BrandColors("#ffffff", "#111111", "#abcdef");

        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(a == b).IsFalse();
    }
}
