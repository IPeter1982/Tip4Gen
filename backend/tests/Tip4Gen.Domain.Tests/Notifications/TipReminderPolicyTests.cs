using Tip4Gen.Domain.Notifications;

namespace Tip4Gen.Domain.Tests.Notifications;

public class TipReminderPolicyTests
{
    private static readonly DateTimeOffset Kickoff = new(2026, 06, 12, 18, 00, 00, TimeSpan.Zero);

    [Fact]
    public void Prefs_disabled_always_returns_none()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(24),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: false,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void Already_tipped_returns_none()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(2),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: true,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void After_deadline_returns_none()
    {
        // Deadline = kickoff − 1h. At T-30m we're past the deadline → no reminder.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromMinutes(30),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void T24h_window_start_inclusive()
    {
        // At exactly kickoff − 25h the window opens.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(25),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.SendT24h, result);
    }

    [Fact]
    public void T24h_window_mid_returns_T24h()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(24),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.SendT24h, result);
    }

    [Fact]
    public void T24h_window_end_exclusive_returns_none()
    {
        // At exactly kickoff − 23h we've exited the T-24h window. T-2h is not yet open.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(23),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void T24h_already_sent_in_window_returns_none()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(24),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: true,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void Outside_either_window_returns_none()
    {
        // Mid-gap (T-10h): too late for T-24h, too early for T-2h.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(10),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void T2h_window_start_inclusive()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(3),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.SendT2h, result);
    }

    [Fact]
    public void T2h_window_mid_returns_T2h()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(2),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.SendT2h, result);
    }

    [Fact]
    public void T2h_window_end_exclusive_returns_none()
    {
        // At exactly kickoff − 1h we're at the deadline; no reminder.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(1),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.None, result);
    }

    [Fact]
    public void T2h_takes_priority_when_both_windows_overlap_intent()
    {
        // Inside T-2h window. Should send T-2h even if T-24h was never sent —
        // last-call beats a stale 24h reminder.
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(2),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: false);
        Assert.Equal(ReminderDecision.SendT2h, result);
    }

    [Fact]
    public void T2h_window_with_T2h_already_sent_returns_none_even_if_T24h_unsent()
    {
        var result = TipReminderPolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(2),
            matchKickoffUtc: Kickoff,
            userPrefsEnabled: true,
            userHasTipped: false,
            alreadySentT24h: false,
            alreadySentT2h: true);
        Assert.Equal(ReminderDecision.None, result);
    }
}
