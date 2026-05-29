using System.ComponentModel.DataAnnotations;

namespace Tip4Gen.Infrastructure.Notifications;

/// <summary>
/// Bound from configuration section "Notifications". Controls the URLs embedded into
/// reminder emails; the rest of the email transport lives in <see cref="ResendOptions"/>.
/// </summary>
public class NotificationsOptions
{
    /// <summary>
    /// Public base URL of the SPA. CTA links in emails point at <c>{SiteBaseUrl}/matches/{id}/tip</c>.
    /// Defaults to local dev; override per-environment.
    /// </summary>
    [Required]
    public string SiteBaseUrl { get; set; } = "http://localhost:5173";
}
