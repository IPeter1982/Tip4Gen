namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Inputs for rendering a tip-reminder email. Pure data; renderer is
/// <see cref="NotificationTemplates"/>.
/// </summary>
public sealed record TipReminderContext(
    string DisplayName,
    string HomeTeamName,
    string AwayTeamName,
    string KickoffBudapestText,
    string DeadlineBudapestText,
    string TipUrl,
    string SiteBaseUrl,
    string PreferencesUrl);
