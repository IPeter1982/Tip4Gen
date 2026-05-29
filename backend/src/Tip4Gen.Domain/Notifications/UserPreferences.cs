namespace Tip4Gen.Domain.Notifications;

/// <summary>
/// Per-user notification preferences. One row per user, keyed by UserId. Defaults to
/// <c>email_reminders_enabled = true</c> in the column default; this entity is only
/// instantiated when the user explicitly opens the settings page or the reminders job
/// finds an existing row.
/// </summary>
public class UserPreferences
{
    public Guid UserId { get; private set; }
    public bool EmailRemindersEnabled { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private UserPreferences() { }

    public UserPreferences(Guid userId, bool emailRemindersEnabled)
    {
        UserId = userId;
        EmailRemindersEnabled = emailRemindersEnabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetEmailReminders(bool enabled)
    {
        if (EmailRemindersEnabled == enabled) return;
        EmailRemindersEnabled = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
