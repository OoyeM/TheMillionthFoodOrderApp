using System.Globalization;
using System.Text;

namespace TheMillionthFoodOrderApp.Application.Orders.Receipts;

/// <summary>
/// Default <see cref="IReceiptComposer"/>: renders a self-contained, email-safe HTML receipt
/// in the order's checkout language (NL/FR/DE), mirroring the printed POS receipt (US-FP-052).
/// Currency and date are formatted with the matching Belgian culture (nl-BE/fr-BE/de-BE) and the
/// shop's time zone. Stateless — safe to register as a singleton.
/// </summary>
public sealed class ReceiptComposer : IReceiptComposer
{
    public ReceiptEmail Compose(OrderResponse order, string? timeZoneId = null)
    {
        var labels = ReceiptLabels.For(order.LanguageCode);
        var culture = ResolveCulture(order.LanguageCode);

        var subject = order.ShopName is { Length: > 0 }
            ? $"{labels.Heading} {order.ShopName} – #{order.OrderNumber}"
            : $"{labels.Heading} – #{order.OrderNumber}";

        var html = BuildHtml(order, labels, culture, timeZoneId);
        return new ReceiptEmail(subject, html);
    }

    private static string BuildHtml(
        OrderResponse order,
        ReceiptLabels labels,
        CultureInfo culture,
        string? timeZoneId)
    {
        string Money(decimal amount) => amount.ToString("C", culture);

        var localDate = ToLocal(order.CreatedAt, timeZoneId);
        var dateText = localDate.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        var vatLabel = string.Format(
            CultureInfo.InvariantCulture,
            labels.VatFormat,
            order.VatRatePercent.ToString("0.##", CultureInfo.InvariantCulture));

        var sb = new StringBuilder();
        sb.Append("<!DOCTYPE html><html><head><meta charset=\"utf-8\">");
        sb.Append("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        sb.Append($"<title>{E(labels.Heading)} #{E(order.OrderNumber)}</title></head>");
        sb.Append("<body style=\"margin:0;padding:24px;background:#f4f4f5;font-family:Arial,Helvetica,sans-serif;color:#18181b;\">");
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"max-width:480px;margin:0 auto;background:#ffffff;border-radius:8px;border:1px solid #e4e4e7;\"><tr><td style=\"padding:24px;\">");

        // Greeting
        sb.Append($"<p style=\"margin:0 0 16px;font-size:16px;\">{E(labels.Greeting)}</p>");

        // Seller legal block
        if (HasSellerBlock(order))
        {
            sb.Append("<div style=\"margin:0 0 16px;font-size:13px;line-height:1.5;color:#3f3f46;\">");
            if (!string.IsNullOrWhiteSpace(order.ShopName))
                sb.Append($"<div style=\"font-weight:bold;color:#18181b;\">{E(order.ShopName)}</div>");
            if (!string.IsNullOrWhiteSpace(order.ShopAddressLine))
                sb.Append($"<div>{E(order.ShopAddressLine)}</div>");
            if (!string.IsNullOrWhiteSpace(order.ShopVatNumber))
                sb.Append($"<div>{E(labels.VatNumber)}: {E(order.ShopVatNumber)}</div>");
            sb.Append("</div>");
        }

        sb.Append("<hr style=\"border:none;border-top:1px solid #e4e4e7;margin:16px 0;\">");

        // Order heading + number
        sb.Append($"<div style=\"font-size:18px;font-weight:bold;text-transform:uppercase;\">{E(labels.Heading)}</div>");
        sb.Append($"<div style=\"font-size:15px;margin:4px 0 12px;\">#{E(order.OrderNumber)}</div>");

        // Meta lines
        sb.Append("<div style=\"font-size:13px;line-height:1.6;color:#3f3f46;\">");
        sb.Append($"<div>{E(labels.OrderTypeLabel(order.OrderType))}</div>");
        if (order.TableNumber is { } table)
            sb.Append($"<div>{E(labels.Table)}: {table.ToString(CultureInfo.InvariantCulture)}</div>");
        if (!string.IsNullOrWhiteSpace(order.CustomerName))
            sb.Append($"<div>{E(labels.Customer)}: {E(order.CustomerName)}</div>");
        sb.Append($"<div>{E(labels.PlacedAt)}: {E(dateText)}</div>");
        sb.Append("</div>");

        sb.Append("<hr style=\"border:none;border-top:1px solid #e4e4e7;margin:16px 0;\">");

        // Items
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"font-size:14px;\">");
        foreach (var item in order.Items)
        {
            sb.Append("<tr><td style=\"padding:4px 0;vertical-align:top;\">");
            sb.Append($"<span style=\"color:#71717a;\">{item.Quantity.ToString(CultureInfo.InvariantCulture)}×</span> {E(item.ProductName)}");
            foreach (var mod in item.SelectedModifiers)
            {
                sb.Append($"<div style=\"font-size:12px;color:#71717a;padding-left:16px;\">+ {E(mod.ModifierName)}");
                if (mod.PriceAdjustment != 0)
                    sb.Append($" ({E(Money(mod.PriceAdjustment))})");
                sb.Append("</div>");
            }
            sb.Append("</td>");
            sb.Append($"<td style=\"padding:4px 0;text-align:right;vertical-align:top;white-space:nowrap;\">{E(Money(item.LineTotal))}</td></tr>");
        }
        sb.Append("</table>");

        sb.Append("<hr style=\"border:none;border-top:1px solid #e4e4e7;margin:16px 0;\">");

        // Totals
        sb.Append("<table role=\"presentation\" width=\"100%\" cellpadding=\"0\" cellspacing=\"0\" style=\"font-size:14px;\">");
        sb.Append(TotalRow(labels.SubtotalNet, Money(order.TotalNet), bold: false));
        sb.Append(TotalRow(vatLabel, Money(order.TotalVatAmount), bold: false));
        sb.Append(TotalRow(labels.Total, Money(order.TotalGross), bold: true));
        sb.Append("</table>");

        // Payment
        sb.Append("<div style=\"font-size:14px;margin-top:12px;\">");
        sb.Append($"{E(labels.PaymentMethod)}: {E(labels.PaymentLabel(order.PaymentMethod))}");
        sb.Append("</div>");

        sb.Append("</td></tr></table></body></html>");
        return sb.ToString();
    }

    private static string TotalRow(string label, string value, bool bold)
    {
        var weight = bold ? "bold" : "normal";
        return $"<tr><td style=\"padding:2px 0;font-weight:{weight};\">{E(label)}</td>" +
               $"<td style=\"padding:2px 0;text-align:right;font-weight:{weight};white-space:nowrap;\">{E(value)}</td></tr>";
    }

    private static bool HasSellerBlock(OrderResponse order) =>
        !string.IsNullOrWhiteSpace(order.ShopName)
        || !string.IsNullOrWhiteSpace(order.ShopAddressLine)
        || !string.IsNullOrWhiteSpace(order.ShopVatNumber);

    private static CultureInfo ResolveCulture(string? languageCode)
    {
        var code = string.IsNullOrWhiteSpace(languageCode) ? "nl-BE" : languageCode.Trim();
        if (!code.Contains('-'))
        {
            code = code.ToLowerInvariant() switch
            {
                "fr" => "fr-BE",
                "de" => "de-BE",
                _ => "nl-BE",
            };
        }

        try
        {
            return CultureInfo.GetCultureInfo(code);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo("nl-BE");
        }
    }

    private static DateTimeOffset ToLocal(DateTimeOffset utc, string? timeZoneId)
    {
        var id = string.IsNullOrWhiteSpace(timeZoneId) ? "Europe/Brussels" : timeZoneId;
        try
        {
            return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById(id));
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            try
            {
                return TimeZoneInfo.ConvertTime(utc, TimeZoneInfo.FindSystemTimeZoneById("Europe/Brussels"));
            }
            catch (Exception fallbackEx) when (fallbackEx is TimeZoneNotFoundException or InvalidTimeZoneException)
            {
                return utc;
            }
        }
    }

    private static string E(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
