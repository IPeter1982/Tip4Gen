namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Provider-agnostic email sender. One call per recipient. Implementation lives in
/// Infrastructure (Resend) so Domain stays network-free.
/// </summary>
public interface INotificationSender
{
    Task<NotificationSendResult> SendAsync(NotificationEmail email, CancellationToken ct);
}
