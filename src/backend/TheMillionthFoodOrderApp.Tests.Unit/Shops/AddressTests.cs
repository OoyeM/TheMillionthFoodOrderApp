using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class AddressTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Equality_IdenticalComponents_AreEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        await Assert.That(address1).IsEqualTo(address2);
        await Assert.That(address1 == address2).IsTrue();
    }

    [Test]
    public async Task Equality_IdenticalComponents_HaveSameHashCode()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        await Assert.That(address1.GetHashCode()).IsEqualTo(address2.GetHashCode());
    }

    // ── Inequality ────────────────────────────────────────────────────────────

    [Test]
    public async Task Inequality_DifferentStreet_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Korenmarkt", "1", "Gent", "9000", "BE");

        await Assert.That(address1).IsNotEqualTo(address2);
        await Assert.That(address1 != address2).IsTrue();
    }

    [Test]
    public async Task Inequality_DifferentNumber_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "2", "Gent", "9000", "BE");

        await Assert.That(address1).IsNotEqualTo(address2);
    }

    [Test]
    public async Task Inequality_DifferentCity_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Brugge", "8000", "BE");

        await Assert.That(address1).IsNotEqualTo(address2);
    }

    [Test]
    public async Task Inequality_DifferentPostalCode_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9001", "BE");

        await Assert.That(address1).IsNotEqualTo(address2);
    }

    [Test]
    public async Task Inequality_DifferentCountry_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "NL");

        await Assert.That(address1).IsNotEqualTo(address2);
    }

    // ── Default country ───────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithoutCountry_DefaultsToBebelgium()
    {
        var address = new Address("Vrijdagmarkt", "1", "Gent", "9000");

        await Assert.That(address.Country).IsEqualTo("BE");
    }

    [Test]
    public async Task Create_WithExplicitCountry_UsesProvidedCountry()
    {
        var address = new Address("Rue de la Loi", "1", "Brussels", "1000", "LU");

        await Assert.That(address.Country).IsEqualTo("LU");
    }

    [Test]
    public async Task Create_WithNonBelgianCountry_UsesProvidedCountry()
    {
        var address = new Address("Dam", "1", "Amsterdam", "1012", "NL");

        await Assert.That(address.Country).IsEqualTo("NL");
    }

    // ── Properties ────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_PersistsAllComponents()
    {
        var address = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        await Assert.That(address.Street).IsEqualTo("Vrijdagmarkt");
        await Assert.That(address.Number).IsEqualTo("1");
        await Assert.That(address.City).IsEqualTo("Gent");
        await Assert.That(address.PostalCode).IsEqualTo("9000");
        await Assert.That(address.Country).IsEqualTo("BE");
    }
}
