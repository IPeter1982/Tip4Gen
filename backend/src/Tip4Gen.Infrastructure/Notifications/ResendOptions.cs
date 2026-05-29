using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Notifications;

/// <summary>
/// Bound from configuration section "Resend". Empty ApiKey is intentional: when unset
/// <see cref="ResendNotificationSender"/> short-circuits to
/// <c>NotificationSendResult.Disabled</c> so the worker can run in dev without
/// credentials. Drop the key via <c>dotnet user-secrets set Resend:ApiKey re_…</c>.
/// </summary>
public class ResendOptions
{
    public string ApiKey { get; set; } = string.Empty;

    [Required]
    public string BaseUrl { get; set; } = "https://api.resend.com";

    [Required]
    public string FromAddress { get; set; } = "Tip4Gen <onboarding@resend.dev>";

    [Range(1, 120)]
    public int TimeoutSeconds { get; set; } = 15;
}
