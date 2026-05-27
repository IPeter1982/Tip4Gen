using Tip4Gen.Domain.Ai;

namespace Tip4Gen.Domain.Tests.Ai;

public class AiTipSchedulePolicyTests
{
    private static readonly DateTimeOffset Kickoff = new(2026, 06, 12, 18, 00, 00, TimeSpan.Zero);

    [Fact]
    public void Tip_already_exists_always_skips()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromMinutes(30),
            kickoffUtc: Kickoff,
            tipExists: true,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.Skip, result);
    }

    [Fact]
    public void Three_hours_before_kickoff_is_too_early()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(3),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.Skip, result);
    }

    [Fact]
    public void At_T_minus_2h_with_no_attempts_yet_attempts_ai()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromHours(2),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.AttemptAi, result);
    }

    [Fact]
    public void In_first_attempt_window_after_one_attempt_skips_until_retry_window()
    {
        // Between T-2h and T-90min, after the first attempt has run we wait.
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromMinutes(100),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 1);
        Assert.Equal(AiTipDecision.Skip, result);
    }

    [Fact]
    public void At_T_minus_90min_with_one_attempt_retries()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromMinutes(90),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 1);
        Assert.Equal(AiTipDecision.AttemptAi, result);
    }

    [Fact]
    public void At_T_minus_90min_with_two_attempts_skips()
    {
        // Both attempts have failed; wait for the fallback window.
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromMinutes(70),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 2);
        Assert.Equal(AiTipDecision.Skip, result);
    }

    [Fact]
    public void At_T_minus_1h_writes_fallback_regardless_of_attempts()
    {
        // Fallback fires the moment we enter T-1h with no tip — independent of how
        // many AI attempts have run.
        foreach (var attempts in new[] { 0, 1, 2 })
        {
            var result = AiTipSchedulePolicy.Decide(
                now: Kickoff - TimeSpan.FromHours(1),
                kickoffUtc: Kickoff,
                tipExists: false,
                previousAttempts: attempts);
            Assert.Equal(AiTipDecision.WriteFallback, result);
        }
    }

    [Fact]
    public void Just_before_kickoff_still_writes_fallback()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff - TimeSpan.FromSeconds(1),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.WriteFallback, result);
    }

    [Fact]
    public void At_kickoff_decision_is_deadline_passed()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff,
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.DeadlinePassed, result);
    }

    [Fact]
    public void After_kickoff_decision_is_deadline_passed()
    {
        var result = AiTipSchedulePolicy.Decide(
            now: Kickoff + TimeSpan.FromHours(1),
            kickoffUtc: Kickoff,
            tipExists: false,
            previousAttempts: 0);
        Assert.Equal(AiTipDecision.DeadlinePassed, result);
    }
}
