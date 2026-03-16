using Shouldly;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class MoneyTests
{
    [Fact]
    public void Create_WithValidData_SetsProperly()
    {
        var money = new Money(3.50m, "EUR");

        money.Amount.ShouldBe(3.50m);
        money.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Create_NormalizesToUpperCase()
    {
        var money = new Money(1.00m, "eur");

        money.Currency.ShouldBe("EUR");
    }

    [Fact]
    public void Create_WithZeroAmount_Succeeds()
    {
        var money = new Money(0m, "EUR");

        money.Amount.ShouldBe(0m);
    }

    [Fact]
    public void Create_WithNegativeAmount_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Money(-1.00m, "EUR"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("EU")]
    [InlineData("EURO")]
    [InlineData("   ")]
    public void Create_WithInvalidCurrency_ThrowsArgumentException(string currency)
    {
        Should.Throw<ArgumentException>(() => new Money(1.00m, currency));
    }

    [Fact]
    public void Create_WithNullCurrency_ThrowsArgumentException()
    {
        Should.Throw<ArgumentException>(() => new Money(1.00m, null!));
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(3.50m, "EUR");

        a.ShouldBe(b);
        (a == b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentValues_AreNotEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(5.00m, "EUR");

        a.ShouldNotBe(b);
        (a != b).ShouldBeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrencies_AreNotEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(3.50m, "USD");

        a.ShouldNotBe(b);
    }
}
