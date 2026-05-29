namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Shape sent to <see cref="INotificationSender"/>. Pure data — no rendering happens here;
/// templates render upstream so the sender stays provider-agnostic.
/// </summary>
public sealed record NotificationEmail(
    string ToEmail,
    string ToDisplayName,
    string Subject,
    string HtmlBody,
    string TextBody);
