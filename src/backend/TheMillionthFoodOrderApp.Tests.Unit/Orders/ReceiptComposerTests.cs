using System.Globalization;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Orders.Receipts;

namespace TheMillionthFoodOrderApp.Tests.Unit.Orders;

/// <summary>
/// Pure unit tests for <see cref="ReceiptComposer.Compose"/>.
/// Verifies HTML structure, localization (NL/FR/DE), currency formatting, VAT rows,
/// seller block presence/absence, and the subject line.
/// All currency assertions use the same <see cref="CultureInfo"/> the composer uses so
/// the tests stay robust across OS/ICU versions.
/// </summary>
public sealed class ReceiptComposerTests
{
    private static readonly ReceiptComposer Composer = new();

    // ── Fixture factory ───────────────────────────────────────────────────────

    /// <summary>
    /// Builds a representative <see cref="OrderResponse"/> with 2 items.
    /// Item 1: 1× Frietje Klein at €3.50 gross, no modifiers.
    /// Item 2: 2× Saus at €1.20 gross each, with one modifier (+€0.30, "Extra Pikant").
    /// Uses Dutch (nl-BE) by default.
    /// </summary>
    private static OrderResponse BuildOrder(string languageCode = "nl-BE") =>
        new(
            Id: Guid.NewGuid(),
            OrderNumber: "TEST-0042",
            ShopId: Guid.NewGuid(),
            BrandSlug: "frietjes",
            OrderType: "Pickup",
            PaymentMethod: "CashAtPickup",
            StatusName: "Placed",
            CustomerName: "Jan Janssen",
            Items: new List<OrderItemResponse>
            {
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Frietje Klein",
                    Quantity: 1,
                    UnitGrossPrice: 3.50m,
                    UnitNetPrice: 3.30m,
                    UnitVatAmount: 0.20m,
                    LineTotal: 3.50m,
                    SelectedModifiers: new List<SelectedModifierResponse>().AsReadOnly()),
                new(
                    ProductId: Guid.NewGuid(),
                    ProductName: "Saus",
                    Quantity: 2,
                    UnitGrossPrice: 1.20m,
                    UnitNetPrice: 1.13m,
                    UnitVatAmount: 0.07m,
                    LineTotal: 2.40m,
                    SelectedModifiers: new List<SelectedModifierResponse>
                    {
                        new(
                            ModifierId: Guid.NewGuid(),
                            ModifierName: "Extra Pikant",
                            PriceAdjustment: 0.30m)
                    }.AsReadOnly())
            }.AsReadOnly(),
            VatRatePercent: 6m,
            SubtotalGross: 5.90m,
            TotalVatAmount: 0.34m,
            TotalNet: 5.56m,
            TotalGross: 5.90m,
            CreatedAt: new DateTimeOffset(2026, 6, 4, 12, 30, 0, TimeSpan.Zero),
            TableNumber: null,
            CreatedByStaffId: null,
            CustomerEmail: "jan@example.com",
            CustomerPhone: "+32470000001",
            ShopName: "Frietjes Brussel",
            ShopVatNumber: "BE0123456789",
            ShopAddressLine: "Frietstraat 1, 1000 Brussel",
            CustomerFirstName: "Jan",
            CustomerLastName: "Janssen",
            LanguageCode: languageCode);

    // ── HTML sanity ───────────────────────────────────────────────────────────

    [Test]
    public async Task Compose_ReturnsHtmlBody()
    {
        var result = Composer.Compose(BuildOrder());

        // Must look like HTML
        await Assert.That(result.HtmlBody).Contains("<html");
        await Assert.That(result.HtmlBody).Contains("<table");
    }

    [Test]
    public async Task Compose_SubjectContainsOrderNumber()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.Subject).Contains("TEST-0042");
    }

    // ── Content: order number ─────────────────────────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsOrderNumber()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("TEST-0042");
    }

    // ── Content: product names ────────────────────────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsProductNames()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("Frietje Klein");
        await Assert.That(result.HtmlBody).Contains("Saus");
    }

    // ── Content: modifier name and formatted price ────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsModifierNameAndPrice()
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var expectedPrice = (0.30m).ToString("C", culture);

        var result = Composer.Compose(BuildOrder("nl-BE"));

        await Assert.That(result.HtmlBody).Contains("Extra Pikant");
        await Assert.That(result.HtmlBody).Contains(expectedPrice);
    }

    // ── Content: line totals ──────────────────────────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsLineTotals()
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var lineTotal1 = (3.50m).ToString("C", culture); // Frietje Klein
        var lineTotal2 = (2.40m).ToString("C", culture); // Saus

        var result = Composer.Compose(BuildOrder("nl-BE"));

        await Assert.That(result.HtmlBody).Contains(lineTotal1);
        await Assert.That(result.HtmlBody).Contains(lineTotal2);
    }

    // ── Content: VAT row (rate + net + vat amount + gross) ───────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsVatRateAndAmounts()
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var totalNet = (5.56m).ToString("C", culture);
        var totalVat = (0.34m).ToString("C", culture);
        var totalGross = (5.90m).ToString("C", culture);

        var result = Composer.Compose(BuildOrder("nl-BE"));

        // VAT rate appears in the label (e.g. "Btw 6%")
        await Assert.That(result.HtmlBody).Contains("6");

        // Net and VAT amounts must appear in the totals block
        await Assert.That(result.HtmlBody).Contains(totalNet);
        await Assert.That(result.HtmlBody).Contains(totalVat);
        await Assert.That(result.HtmlBody).Contains(totalGross);
    }

    // ── Content: payment method ───────────────────────────────────────────────

    [Test]
    public async Task Compose_NL_HtmlBodyContainsLocalizedPaymentLabel()
    {
        var result = Composer.Compose(BuildOrder("nl-BE"));

        // Dutch label for CashAtPickup = "Contant"
        await Assert.That(result.HtmlBody).Contains("Contant");
    }

    [Test]
    public async Task Compose_FR_HtmlBodyContainsLocalizedPaymentLabel()
    {
        var result = Composer.Compose(BuildOrder("fr-BE"));

        // French label for CashAtPickup = "Espèces"
        await Assert.That(result.HtmlBody).Contains("Esp");
    }

    [Test]
    public async Task Compose_DE_HtmlBodyContainsLocalizedPaymentLabel()
    {
        var result = Composer.Compose(BuildOrder("de-BE"));

        // German label for CashAtPickup = "Bar"
        await Assert.That(result.HtmlBody).Contains("Bar");
    }

    // ── Content: date (dd/MM/yyyy format) ────────────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsFormattedDate()
    {
        // The CreatedAt is 2026-06-04T12:30:00Z. In Europe/Brussels (UTC+2 in summer)
        // it converts to 14:30, so the date part is still "04/06/2026".
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("04/06/2026");
    }

    // ── Content: seller legal block ───────────────────────────────────────────

    [Test]
    public async Task Compose_HtmlBodyContainsShopName()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("Frietjes Brussel");
    }

    [Test]
    public async Task Compose_HtmlBodyContainsShopAddressLine()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("Frietstraat 1, 1000 Brussel");
    }

    [Test]
    public async Task Compose_HtmlBodyContainsShopVatNumber()
    {
        var result = Composer.Compose(BuildOrder());

        await Assert.That(result.HtmlBody).Contains("BE0123456789");
    }

    // ── Localization: NL heading ──────────────────────────────────────────────

    [Test]
    public async Task Compose_NL_HtmlBodyContainsKasticketHeading()
    {
        var result = Composer.Compose(BuildOrder("nl-BE"));

        await Assert.That(result.HtmlBody).Contains("Kasticket");
    }

    [Test]
    public async Task Compose_NL_SubjectContainsKasticket()
    {
        var result = Composer.Compose(BuildOrder("nl-BE"));

        await Assert.That(result.Subject).Contains("Kasticket");
    }

    // ── Localization: FR heading ──────────────────────────────────────────────

    [Test]
    public async Task Compose_FR_HtmlBodyContainsTicketDeCaisseHeading()
    {
        var result = Composer.Compose(BuildOrder("fr-BE"));

        await Assert.That(result.HtmlBody).Contains("Ticket de caisse");
    }

    [Test]
    public async Task Compose_FR_SubjectContainsTicketDeCaisse()
    {
        var result = Composer.Compose(BuildOrder("fr-BE"));

        await Assert.That(result.Subject).Contains("Ticket de caisse");
    }

    // ── Localization: DE heading ──────────────────────────────────────────────

    [Test]
    public async Task Compose_DE_HtmlBodyContainsKassenbonHeading()
    {
        var result = Composer.Compose(BuildOrder("de-BE"));

        await Assert.That(result.HtmlBody).Contains("Kassenbon");
    }

    [Test]
    public async Task Compose_DE_SubjectContainsKassenbon()
    {
        var result = Composer.Compose(BuildOrder("de-BE"));

        await Assert.That(result.Subject).Contains("Kassenbon");
    }

    // ── Localization: 2-letter code fallback ─────────────────────────────────

    [Test]
    public async Task Compose_TwoLetterNl_ResolvesToDutch()
    {
        var result = Composer.Compose(BuildOrder("nl"));

        await Assert.That(result.HtmlBody).Contains("Kasticket");
    }

    [Test]
    public async Task Compose_TwoLetterFr_ResolvesToFrench()
    {
        var result = Composer.Compose(BuildOrder("fr"));

        await Assert.That(result.HtmlBody).Contains("Ticket de caisse");
    }

    [Test]
    public async Task Compose_TwoLetterDe_ResolvesToGerman()
    {
        var result = Composer.Compose(BuildOrder("de"));

        await Assert.That(result.HtmlBody).Contains("Kassenbon");
    }

    // ── Seller block: null VatNumber → omitted from HTML ─────────────────────

    [Test]
    public async Task Compose_WhenShopVatNumberIsNull_VatNumberLineOmitted()
    {
        // Build an order without a VAT number on the shop.
        var orderWithoutVat = BuildOrder() with { ShopVatNumber = null };

        var result = Composer.Compose(orderWithoutVat);

        // The VAT-number line must not appear; other seller-block info still present.
        await Assert.That(result.HtmlBody).DoesNotContain("BE0123456789");
        await Assert.That(result.HtmlBody).Contains("Frietjes Brussel");
    }
}
