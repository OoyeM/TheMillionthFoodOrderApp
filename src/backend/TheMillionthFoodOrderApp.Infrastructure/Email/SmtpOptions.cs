namespace TheMillionthFoodOrderApp.Infrastructure.Email;

/// <summary>
/// Configuration for <see cref="SmtpEmailSender"/>, bound from the "Email" configuration section.
/// In dev, Aspire injects <see cref="Host"/>/<see cref="Port"/> from the mailpit container.
/// In prod, point these at a real SMTP relay — no code change required.
/// </summary>
public sealed class SmtpOptions
{
    /// <summary>When false, the sender no-ops and logs (mirrors the Authentication dev toggle).</summary>
    public bool Enabled { get; set; } = true;

    public string Host { get; set; } = "localhost";

    public int Port { get; set; } = 1025;

    /// <summary>Use STARTTLS. Off for the local mailpit catcher; on for most prod relays.</summary>
    public bool UseSsl { get; set; }

    public string FromAddress { get; set; } = "no-reply@frietjes.local";

    public string FromName { get; set; } = "Frietjes?";

    /// <summary>Optional SMTP auth — left null for the local catcher.</summary>
    public string? UserName { get; set; }

    public string? Password { get; set; }
}
