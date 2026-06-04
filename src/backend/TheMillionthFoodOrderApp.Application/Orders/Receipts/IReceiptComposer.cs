namespace TheMillionthFoodOrderApp.Application.Orders.Receipts;

/// <summary>
/// Renders the digital receipt email (US-FP-051) from an <see cref="OrderResponse"/>.
/// Pure (no I/O): produces a localized subject + HTML body in the order's checkout language,
/// mirroring the layout of the printed POS receipt (US-FP-052).
/// </summary>
public interface IReceiptComposer
{
    /// <param name="order">The order to render. <see cref="OrderResponse.LanguageCode"/> selects the language.</param>
    /// <param name="timeZoneId">
    /// IANA time zone of the shop (e.g. "Europe/Brussels") used to render the order date/time.
    /// Falls back to Europe/Brussels, then UTC, when null or unknown.
    /// </param>
    ReceiptEmail Compose(OrderResponse order, string? timeZoneId = null);
}

/// <summary>The rendered receipt email: a localized subject line and an HTML body.</summary>
public sealed record ReceiptEmail(string Subject, string HtmlBody);
