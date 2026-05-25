using Tip4Gen.Domain.Tipping;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Tipping;

public class TipRulesValidatorTests
{
    private static readonly DateTimeOffset Now = new(2026, 06, 12, 18, 00, 00, TimeSpan.Zero);
    // Default kickoff: well after `Now` so the deadline check passes.
    private static readonly DateTimeOffset FutureKickoff = Now.AddHours(6);

    [Fact]
    public void Valid_tip_well_before_deadline_passes()
    {
        var result = TipRulesValidator.Validate(
            now: Now,
            matchKickoffUtc: FutureKickoff,
            matchStage: Stage.Group,
            homeGoals: 2,
            awayGoals: 1,
            usingJoker: false,
            otherJokerCountForUser: 0);

        Assert.True(result.IsValid);
        Assert.Equal(TipRejectionReason.None, result.Reason);
    }

    [Fact]
    public void Tip_exactly_at_deadline_is_rejected()
    {
        // Deadline is kickoff - 1h; submitting at exactly that moment must fail.
        var result = TipRulesValidator.Validate(
            now: FutureKickoff - TimeSpan.FromHours(1),
            matchKickoffUtc: FutureKickoff,
            matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: false, otherJokerCountForUser: 0);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.DeadlinePassed, result.Reason);
    }

    [Fact]
    public void Tip_one_second_before_deadline_passes()
    {
        var result = TipRulesValidator.Validate(
            now: FutureKickoff - TimeSpan.FromHours(1) - TimeSpan.FromSeconds(1),
            matchKickoffUtc: FutureKickoff,
            matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: false, otherJokerCountForUser: 0);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Tip_after_kickoff_is_rejected()
    {
        var result = TipRulesValidator.Validate(
            now: FutureKickoff.AddMinutes(30),
            matchKickoffUtc: FutureKickoff,
            matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: false, otherJokerCountForUser: 0);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.DeadlinePassed, result.Reason);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(16, 0)]
    [InlineData(0, 16)]
    [InlineData(100, 100)]
    public void Out_of_range_scores_are_rejected(int home, int away)
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Group,
            homeGoals: home, awayGoals: away, usingJoker: false, otherJokerCountForUser: 0);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.ScoreOutOfRange, result.Reason);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(15, 15)]
    [InlineData(7, 3)]
    public void Boundary_scores_are_accepted(int home, int away)
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Group,
            homeGoals: home, awayGoals: away, usingJoker: false, otherJokerCountForUser: 0);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(Stage.R32)]
    [InlineData(Stage.R16)]
    [InlineData(Stage.QF)]
    [InlineData(Stage.SF)]
    [InlineData(Stage.Bronze)]
    [InlineData(Stage.Final)]
    public void Joker_on_knockout_match_is_rejected(Stage knockout)
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: knockout,
            homeGoals: 1, awayGoals: 0, usingJoker: true, otherJokerCountForUser: 0);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.JokerNotAllowedOnKnockoutMatch, result.Reason);
    }

    [Fact]
    public void Joker_on_group_match_within_quota_is_accepted()
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: true, otherJokerCountForUser: 2);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Joker_when_three_already_used_elsewhere_is_rejected()
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: true, otherJokerCountForUser: 3);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.JokerQuotaExceeded, result.Reason);
    }

    [Fact]
    public void Non_joker_tip_ignores_quota()
    {
        // Quota only checked when usingJoker is true.
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Group,
            homeGoals: 1, awayGoals: 0, usingJoker: false, otherJokerCountForUser: 99);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Non_joker_on_knockout_is_allowed()
    {
        var result = TipRulesValidator.Validate(
            now: Now, matchKickoffUtc: FutureKickoff, matchStage: Stage.Final,
            homeGoals: 1, awayGoals: 0, usingJoker: false, otherJokerCountForUser: 0);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Deadline_check_runs_before_score_check()
    {
        // If multiple rules are violated, deadline wins (it's the user-visible primary).
        var result = TipRulesValidator.Validate(
            now: FutureKickoff,
            matchKickoffUtc: FutureKickoff,
            matchStage: Stage.Group,
            homeGoals: 99, awayGoals: 99,
            usingJoker: true, otherJokerCountForUser: 99);

        Assert.False(result.IsValid);
        Assert.Equal(TipRejectionReason.DeadlinePassed, result.Reason);
    }
}
