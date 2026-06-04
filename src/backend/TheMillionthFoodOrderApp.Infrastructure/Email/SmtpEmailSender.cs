using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using TheMillionthFoodOrderApp.Application.Email;

namespace TheMillionthFoodOrderApp.Infrastructure.Email;

/// <summary>
/// MailKit-based SMTP implementation of <see cref="IEmailSender"/>.
/// Depends only on <see cref="SmtpOptions"/> + <see cref="ILogger{T}"/>, so it can be registered
/// as a singleton. Builds a multipart HTML(+text) message and sends it over SMTP — to a mailpit
/// catcher in dev, to any SMTP relay in prod.
/// </summary>
public sealed class SmtpEmailSender(
    IOptions<SmtpOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            logger.LogInformation(
                "Email sending is disabled — skipping email to {To} ('{Subject}').",
                message.To, message.Subject);
            return;
        }

        var mime = new MimeMessage();
        mime.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        mime.To.Add(MailboxAddress.Parse(message.To));
        mime.Subject = message.Subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = message.HtmlBody };
        if (!string.IsNullOrWhiteSpace(message.PlainTextBody))
            bodyBuilder.TextBody = message.PlainTextBody;
        mime.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

        var socketOptions = _options.UseSsl
            ? SecureSocketOptions.StartTls
            : SecureSocketOptions.None;

        await client.ConnectAsync(_options.Host, _options.Port, socketOptions, cancellationToken);

        if (!string.IsNullOrWhiteSpace(_options.UserName))
            await client.AuthenticateAsync(_options.UserName, _options.Password, cancellationToken);

        await client.SendAsync(mime, cancellationToken);
        await client.DisconnectAsync(quit: true, cancellationToken);

        logger.LogInformation("Sent email to {To} ('{Subject}').", message.To, message.Subject);
    }
}
