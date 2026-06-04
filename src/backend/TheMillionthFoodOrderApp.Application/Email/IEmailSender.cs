namespace TheMillionthFoodOrderApp.Application.Email;

/// <summary>
/// Vendor-neutral transport abstraction for sending emails (US-FP-051).
/// The implementation lives in Infrastructure (SMTP via MailKit, pointed at a mailpit
/// catcher in dev and any SMTP relay in prod via config — a config-only swap, mirroring
/// the LocalFileStorage → Azure Blob and Wolverine in-memory → broker story).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default);
}

/// <summary>A single outbound email. Always carries an HTML body; plain text is optional.</summary>
public sealed record EmailMessage(
    string To,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null);
