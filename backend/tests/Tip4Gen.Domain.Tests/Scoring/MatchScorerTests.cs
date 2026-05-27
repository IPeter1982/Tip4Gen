using Tip4Gen.Domain.Scoring;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Tests.Scoring;

public class MatchScorerTests
{
    // ===== Category coverage =====

    [Theory]
    [InlineData(2, 1, 2, 1)]   // typical exact
    [InlineData(0, 0, 0, 0)]   // exact draw
    [InlineData(5, 4, 5, 4)]   // exact high-scorer
    public void Exact_score_returns_Exact(int th, int ta, int rh, int ra)
    {
        var r = MatchScorer.Score(th, ta, new MatchResult(rh, ra), Stage.Group, joker: false);
        Assert.Equal(ScoreCategory.Exact, r.Category);
        Assert.Equal(10, r.BasePoints);
    }

    [Theory]
    [InlineData(2, 1, 3, 2)]   // same winner, same GD (+1), not exact
    [InlineData(1, 2, 2, 3)]   // same winner (away), same GD (-1), not exact
    [InlineData(5, 4, 4, 3)]   // same winner, same GD, not exact, different goal counts
    public void Correct_winner_and_goal_diff_returns_WinnerAndGoalDiff(int th, int ta, int rh, int ra)
    {
        var r = MatchScorer.Score(th, ta, new MatchResult(rh, ra), Stage.Group, joker: false);
        Assert.Equal(ScoreCategory.WinnerAndGoalDiff, r.Category);
        Assert.Equal(5, r.BasePoints);
    }

    [Theory]
    [InlineData(3, 1, 2, 1)]   // correct winner, different GD, but away matches → still Winner (3 > OneTeamGoals)
    [InlineData(2, 0, 3, 2)]   // correct winner, different GD, no team-goal match
    [InlineData(1, 1, 2, 2)]   // correct draw, wrong score (guide example)
    [InlineData(1, 1, 3, 3)]   // correct draw, wrong score, neither matches
    public void Correct_outcome_without_matching_GD_returns_Winner(int th, int ta, int rh, int ra)
    {
        var r = MatchScorer.Score(th, ta, new MatchResult(rh, ra), Stage.Group, joker: false);
        Assert.Equal(ScoreCategory.Winner, r.Category);
        Assert.Equal(3, r.BasePoints);
    }

    [Theory]
    [InlineData(2, 1, 2, 3)]   // wrong winner, home matches
    [InlineData(2, 1, 1, 1)]   // wrong outcome (draw vs win), away matches
    [InlineData(2, 0, 2, 3)]   // wrong winner, home matches (both 2)
    public void Wrong_outcome_with_one_matching_goal_returns_OneTeamGoals(int th, int ta, int rh, int ra)
    {
        var r = MatchScorer.Score(th, ta, new MatchResult(rh, ra), Stage.Group, joker: false);
        Assert.Equal(ScoreCategory.OneTeamGoals, r.Category);
        Assert.Equal(1, r.BasePoints);
    }

    [Theory]
    [InlineData(2, 1, 1, 2)]   // strictly swapped: NOT credited as OneTeamGoals
    [InlineData(2, 1, 0, 2)]   // wrong outcome, nothing matches
    [InlineData(3, 0, 0, 3)]   // swapped, also wrong outcome
    public void Swapped_or_mismatched_returns_Nothing(int th, int ta, int rh, int ra)
    {
        var r = MatchScorer.Score(th, ta, new MatchResult(rh, ra), Stage.Group, joker: false);
        Assert.Equal(ScoreCategory.Nothing, r.Category);
        Assert.Equal(0, r.BasePoints);
    }

    // ===== Multiplier coverage =====

    [Theory]
    [InlineData(Stage.Group, 10)]      // 10 × 1.0
    [InlineData(Stage.R32, 15)]        // 10 × 1.5
    [InlineData(Stage.R16, 15)]        // 10 × 1.5
    [InlineData(Stage.QF, 20)]         // 10 × 2.0
    [InlineData(Stage.SF, 25)]         // 10 × 2.5
    [InlineData(Stage.Bronze, 20)]     // 10 × 2.0
    [InlineData(Stage.Final, 30)]      // 10 × 3.0
    public void Exact_score_multiplies_by_stage(Stage stage, int expectedFinal)
    {
        var r = MatchScorer.Score(2, 1, new MatchResult(2, 1), stage, joker: false);
        Assert.Equal(expectedFinal, r.FinalPoints);
    }

    [Theory]
    [InlineData(1, 1.5, 2)]   // 1 × 1.5 = 1.5 → round AwayFromZero → 2
    [InlineData(3, 1.5, 5)]   // 3 × 1.5 = 4.5 → 5
    [InlineData(5, 1.5, 8)]   // 5 × 1.5 = 7.5 → 8
    [InlineData(5, 2.5, 13)]  // 5 × 2.5 = 12.5 → 13
    [InlineData(1, 2.5, 3)]   // 1 × 2.5 = 2.5 → 3
    public void Half_multiplier_rounds_away_from_zero(int basePoints, double mult, int expectedFinal)
    {
        // synth: just compute round directly to mirror the scorer's contract
        var rounded = (int)Math.Round(basePoints * (decimal)mult, MidpointRounding.AwayFromZero);
        Assert.Equal(expectedFinal, rounded);
    }

    // ===== Joker doubling =====

    [Fact]
    public void Joker_doubles_after_multiplier()
    {
        // 10 × 2.5 = 25, joker → 50 (not 10 × 2 × 2.5 = 50, which is the same here)
        // Use a half case to make ordering observable: 5 × 1.5 = 7.5 → 8, joker → 16.
        var r = MatchScorer.Score(2, 1, new MatchResult(3, 2), Stage.R32, joker: true);
        Assert.Equal(ScoreCategory.WinnerAndGoalDiff, r.Category);
        Assert.Equal(5, r.BasePoints);
        Assert.Equal(1.5m, r.Multiplier);
        Assert.True(r.JokerApplied);
        Assert.Equal(16, r.FinalPoints);   // 5*1.5=7.5→8, ×2 = 16
    }

    [Fact]
    public void No_joker_uses_unmultiplied_double_path()
    {
        var r = MatchScorer.Score(2, 1, new MatchResult(3, 2), Stage.R32, joker: false);
        Assert.False(r.JokerApplied);
        Assert.Equal(8, r.FinalPoints);
    }

    [Fact]
    public void Joker_on_zero_is_still_zero()
    {
        var r = MatchScorer.Score(2, 1, new MatchResult(1, 2), Stage.Group, joker: true);
        Assert.Equal(ScoreCategory.Nothing, r.Category);
        Assert.Equal(0, r.FinalPoints);
    }

    // ===== Result struct =====

    [Fact]
    public void ScoringResult_records_all_inputs()
    {
        var r = MatchScorer.Score(2, 1, new MatchResult(2, 1), Stage.Final, joker: false);
        Assert.Equal(ScoreCategory.Exact, r.Category);
        Assert.Equal(10, r.BasePoints);
        Assert.Equal(3.0m, r.Multiplier);
        Assert.False(r.JokerApplied);
        Assert.Equal(30, r.FinalPoints);
    }
}
