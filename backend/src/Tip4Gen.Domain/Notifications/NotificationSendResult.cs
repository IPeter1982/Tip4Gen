namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Outcome of one <see cref="INotificationSender"/> call. The orchestrator persists
/// success vs. failure to <c>notification_log</c> so dedup + retry can be reasoned about
/// across job restarts.
/// </summary>
public abstract record NotificationSendResult
{
    public sealed record Success(string ProviderMessageId) : NotificationSendResult;

    /// <summary>Sender configured but no API key — treat as a no-op so the worker can
    /// run without credentials in dev. Don't persist a log row.</summary>
    public sealed record Disabled : NotificationSendResult;

    /// <summary>Provider returned 4xx/5xx or threw — log + skip the retry-quota check.</summary>
    public sealed record Failed(string Error) : NotificationSendResult;

    /// <summary>Provider 429 — log + back off; the next tick may try again.</summary>
    public sealed record RateLimited(string Error) : NotificationSendResult;
}
