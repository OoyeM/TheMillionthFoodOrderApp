namespace TheMillionthFoodOrderApp.Application.Orders.Receipts;

/// <summary>
/// Localized label set for the digital receipt, keyed by 2-letter language code.
/// Values are kept verbatim in sync with the frontend <c>pos.receipt.*</c> i18n keys
/// (US-FP-052) so the emailed receipt reads identically to the printed one.
/// </summary>
internal sealed record ReceiptLabels(
    string Greeting,
    string Heading,
    string OrderWord,
    string VatNumber,
    string Table,
    string Customer,
    string PlacedAt,
    string SubtotalNet,
    string VatFormat,
    string Total,
    string PaymentMethod,
    IReadOnlyDictionary<string, string> OrderType,
    IReadOnlyDictionary<string, string> Payment)
{
    /// <summary>Resolves the label set for a BCP-47 or 2-letter code; falls back to Dutch.</summary>
    public static ReceiptLabels For(string? languageCode)
    {
        var code = (languageCode ?? string.Empty).Split('-', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()?.ToLowerInvariant();

        return code switch
        {
            "fr" => French,
            "de" => German,
            _ => Dutch,
        };
    }

    private static readonly ReceiptLabels Dutch = new(
        Greeting: "Bedankt voor je bestelling!",
        Heading: "Kasticket",
        OrderWord: "bestelling",
        VatNumber: "Btw-nr.",
        Table: "Tafel",
        Customer: "Klant",
        PlacedAt: "Datum",
        SubtotalNet: "Totaal excl. btw",
        VatFormat: "Btw {0}%",
        Total: "Totaal incl. btw",
        PaymentMethod: "Betaling",
        OrderType: new Dictionary<string, string>
        {
            ["Pickup"] = "Afhalen",
            ["EatIn"] = "Ter plaatse",
            ["Delivery"] = "Bezorging",
        },
        Payment: new Dictionary<string, string>
        {
            ["CashAtPickup"] = "Contant",
            ["CreditCard"] = "Kredietkaart",
            ["Bancontact"] = "Bancontact",
        });

    private static readonly ReceiptLabels French = new(
        Greeting: "Merci pour votre commande !",
        Heading: "Ticket de caisse",
        OrderWord: "commande",
        VatNumber: "N° TVA",
        Table: "Table",
        Customer: "Client",
        PlacedAt: "Date",
        SubtotalNet: "Total hors TVA",
        VatFormat: "TVA {0}%",
        Total: "Total TTC",
        PaymentMethod: "Paiement",
        OrderType: new Dictionary<string, string>
        {
            ["Pickup"] = "À emporter",
            ["EatIn"] = "Sur place",
            ["Delivery"] = "Livraison",
        },
        Payment: new Dictionary<string, string>
        {
            ["CashAtPickup"] = "Espèces",
            ["CreditCard"] = "Carte de crédit",
            ["Bancontact"] = "Bancontact",
        });

    private static readonly ReceiptLabels German = new(
        Greeting: "Vielen Dank für Ihre Bestellung!",
        Heading: "Kassenbon",
        OrderWord: "Bestellung",
        VatNumber: "USt-IdNr.",
        Table: "Tisch",
        Customer: "Kunde",
        PlacedAt: "Datum",
        SubtotalNet: "Summe ohne MwSt.",
        VatFormat: "MwSt. {0}%",
        Total: "Summe inkl. MwSt.",
        PaymentMethod: "Zahlung",
        OrderType: new Dictionary<string, string>
        {
            ["Pickup"] = "Abholung",
            ["EatIn"] = "Vor Ort",
            ["Delivery"] = "Lieferung",
        },
        Payment: new Dictionary<string, string>
        {
            ["CashAtPickup"] = "Bar",
            ["CreditCard"] = "Kreditkarte",
            ["Bancontact"] = "Bancontact",
        });

    /// <summary>Resolves a display label for an order-type enum name, falling back to the raw value.</summary>
    public string OrderTypeLabel(string orderType) =>
        OrderType.TryGetValue(orderType, out var label) ? label : orderType;

    /// <summary>Resolves a display label for a payment-method enum name, falling back to the raw value.</summary>
    public string PaymentLabel(string paymentMethod) =>
        Payment.TryGetValue(paymentMethod, out var label) ? label : paymentMethod;
}
