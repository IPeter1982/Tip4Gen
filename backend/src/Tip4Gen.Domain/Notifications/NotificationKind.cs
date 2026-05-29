namespace Tip4Gen.Domain.Notifications;

public enum NotificationKind
{
    /// <summary>~24h before kickoff: "don't forget" reminder.</summary>
    TipReminder24h = 1,

    /// <summary>~2h before kickoff: last-call reminder (deadline is kickoff − 1h).</summary>
    TipReminder2h = 2,
}
