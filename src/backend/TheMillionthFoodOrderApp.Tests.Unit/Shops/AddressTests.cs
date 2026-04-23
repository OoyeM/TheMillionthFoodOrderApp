using Shouldly;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

public sealed class AddressTests
{
    // ── Equality ──────────────────────────────────────────────────────────────

    [Fact]
    public void Equality_IdenticalComponents_AreEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        address1.ShouldBe(address2);
        (address1 == address2).ShouldBeTrue();
    }

    [Fact]
    public void Equality_IdenticalComponents_HaveSameHashCode()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        address1.GetHashCode().ShouldBe(address2.GetHashCode());
    }

    // ── Inequality ────────────────────────────────────────────────────────────

    [Fact]
    public void Inequality_DifferentStreet_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Korenmarkt", "1", "Gent", "9000", "BE");

        address1.ShouldNotBe(address2);
        (address1 != address2).ShouldBeTrue();
    }

    [Fact]
    public void Inequality_DifferentNumber_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "2", "Gent", "9000", "BE");

        address1.ShouldNotBe(address2);
    }

    [Fact]
    public void Inequality_DifferentCity_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Brugge", "8000", "BE");

        address1.ShouldNotBe(address2);
    }

    [Fact]
    public void Inequality_DifferentPostalCode_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9001", "BE");

        address1.ShouldNotBe(address2);
    }

    [Fact]
    public void Inequality_DifferentCountry_AreNotEqual()
    {
        var address1 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");
        var address2 = new Address("Vrijdagmarkt", "1", "Gent", "9000", "NL");

        address1.ShouldNotBe(address2);
    }

    // ── Default country ───────────────────────────────────────────────────────

    [Fact]
    public void Create_WithoutCountry_DefaultsToBebelgium()
    {
        var address = new Address("Vrijdagmarkt", "1", "Gent", "9000");

        address.Country.ShouldBe("BE");
    }

    [Fact]
    public void Create_WithExplicitCountry_UsesProvidedCountry()
    {
        var address = new Address("Rue de la Loi", "1", "Brussels", "1000", "LU");

        address.Country.ShouldBe("LU");
    }

    [Fact]
    public void Create_WithNonBelgianCountry_UsesProvidedCountry()
    {
        var address = new Address("Dam", "1", "Amsterdam", "1012", "NL");

        address.Country.ShouldBe("NL");
    }

    // ── Properties ────────────────────────────────────────────────────────────

    [Fact]
    public void Create_PersistsAllComponents()
    {
        var address = new Address("Vrijdagmarkt", "1", "Gent", "9000", "BE");

        address.Street.ShouldBe("Vrijdagmarkt");
        address.Number.ShouldBe("1");
        address.City.ShouldBe("Gent");
        address.PostalCode.ShouldBe("9000");
        address.Country.ShouldBe("BE");
    }
}
