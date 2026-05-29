namespace Tip4Gen.Domain.Notifications;

public enum ReminderDecision
{
    /// <summary>No email this tick.</summary>
    None = 0,

    /// <summary>~24h-out reminder.</summary>
    SendT24h = 1,

    /// <summary>~2h-out last-call reminder (deadline is kickoff − 1h).</summary>
    SendT2h = 2,
}

/// <summary>
/// Decides whether to send a tip reminder for a single (user, match) pair on a given
/// tick. Pure — the worker fetches inputs and persists the chosen outcome.
///
/// Windows:
/// • T-24h reminder fires once in [kickoff − 25h, kickoff − 23h)
/// • T-2h  reminder fires once in [kickoff − 3h,  kickoff − 1h)
///
/// Both windows are ~2h wide so a 10-min worker tick lands at least once. Past
/// the deadline (kickoff − 1h) we stop trying. Already-sent reminders dedup via
/// notification_log; the worker passes the boolean in.
/// </summary>
public static class TipReminderPolicy
{
    public static readonly TimeSpan TipDeadlineBeforeKickoff = TimeSpan.FromHours(1);

    public static readonly TimeSpan T24hWindowStart = TimeSpan.FromHours(25);
    public static readonly TimeSpan T24hWindowEnd = TimeSpan.FromHours(23);

    public static readonly TimeSpan T2hWindowStart = TimeSpan.FromHours(3);
    public static readonly TimeSpan T2hWindowEnd = TimeSpan.FromHours(1);

    public static ReminderDecision Decide(
        DateTimeOffset now,
        DateTimeOffset matchKickoffUtc,
        bool userPrefsEnabled,
        bool userHasTipped,
        bool alreadySentT24h,
        bool alreadySentT2h)
    {
        if (!userPrefsEnabled) return ReminderDecision.None;
        if (userHasTipped) return ReminderDecision.None;

        var deadline = matchKickoffUtc - TipDeadlineBeforeKickoff;
        if (now >= deadline) return ReminderDecision.None;

        // T-2h takes priority — it's the more urgent message and we'd rather skip a
        // late T-24h than miss the last-call.
        var t2Start = matchKickoffUtc - T2hWindowStart;
        var t2End = matchKickoffUtc - T2hWindowEnd;
        if (now >= t2Start && now < t2End && !alreadySentT2h)
            return ReminderDecision.SendT2h;

        var t24Start = matchKickoffUtc - T24hWindowStart;
        var t24End = matchKickoffUtc - T24hWindowEnd;
        if (now >= t24Start && now < t24End && !alreadySentT24h)
            return ReminderDecision.SendT24h;

        return ReminderDecision.None;
    }
}
