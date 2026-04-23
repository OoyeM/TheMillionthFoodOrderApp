using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Tests.Unit.Products;

public sealed class MoneyTests
{
    [Test]
    public async Task Create_WithValidData_SetsProperly()
    {
        var money = new Money(3.50m, "EUR");

        await Assert.That(money.Amount).IsEqualTo(3.50m);
        await Assert.That(money.Currency).IsEqualTo("EUR");
    }

    [Test]
    public async Task Create_NormalizesToUpperCase()
    {
        var money = new Money(1.00m, "eur");

        await Assert.That(money.Currency).IsEqualTo("EUR");
    }

    [Test]
    public async Task Create_WithZeroAmount_Succeeds()
    {
        var money = new Money(0m, "EUR");

        await Assert.That(money.Amount).IsEqualTo(0m);
    }

    [Test]
    public async Task Create_WithNegativeAmount_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new Money(-1.00m, "EUR")));
    }

    [Arguments("")]
    [Arguments("EU")]
    [Arguments("EURO")]
    [Arguments("   ")]
    [Test]
    public async Task Create_WithInvalidCurrency_ThrowsArgumentException(string currency)
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new Money(1.00m, currency)));
    }

    [Test]
    public async Task Create_WithNullCurrency_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            Task.FromResult(new Money(1.00m, null!)));
    }

    [Test]
    public async Task Equality_SameValues_AreEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(3.50m, "EUR");

        await Assert.That(a).IsEqualTo(b);
        await Assert.That(a == b).IsTrue();
    }

    [Test]
    public async Task Equality_DifferentValues_AreNotEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(5.00m, "EUR");

        await Assert.That(a).IsNotEqualTo(b);
        await Assert.That(a != b).IsTrue();
    }

    [Test]
    public async Task Equality_DifferentCurrencies_AreNotEqual()
    {
        var a = new Money(3.50m, "EUR");
        var b = new Money(3.50m, "USD");

        await Assert.That(a).IsNotEqualTo(b);
    }
}
