using System.Collections.Concurrent;
using TheMillionthFoodOrderApp.Application.Email;

namespace TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

/// <summary>
/// Thread-safe in-memory <see cref="IEmailSender"/> that records every sent message.
/// Registered as a singleton in <see cref="IntegrationTestWebAppFactory"/> so tests can
/// resolve it from DI and inspect what was sent.
/// </summary>
public sealed class RecordingEmailSender : IEmailSender
{
    private readonly ConcurrentBag<EmailMessage> _sent = new();

    /// <summary>All messages that have been sent, in no guaranteed order.</summary>
    public IReadOnlyList<EmailMessage> SentMessages => _sent.ToList().AsReadOnly();

    public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        _sent.Add(message);
        return Task.CompletedTask;
    }

    /// <summary>Clears all recorded messages (useful for test isolation when the sender is shared).</summary>
    public void Clear() => _sent.Clear();
}
