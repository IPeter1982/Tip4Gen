using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Domain.Scoring;

/// <summary>
/// Pure scoring per guide §3 (categories) + §4 (multipliers) + §6 (joker doubling).
///
/// Contract: this returns the points the tip *earns* given the inputs. It does NOT
/// validate that the joker was legal on the match — that's TipRulesValidator's job
/// at submission time. If a Joker=true tip somehow lands on a knockout match here,
/// the doubling is still applied as instructed; treat upstream validation as the gate.
/// </summary>
public static class MatchScorer
{
    public static ScoringResult Score(int tipHome, int tipAway, MatchResult result, Stage stage, bool joker)
    {
        var category = Categorize(tipHome, tipAway, result);
        var basePoints = (int)category;
        var multiplier = StageMultipliers.For(stage);
        var afterMultiplier = (int)Math.Round(basePoints * multiplier, MidpointRounding.AwayFromZero);
        var final = joker ? afterMultiplier * 2 : afterMultiplier;
        return new ScoringResult(category, basePoints, multiplier, joker, final);
    }

    private static ScoreCategory Categorize(int tipHome, int tipAway, MatchResult result)
    {
        if (tipHome == result.HomeGoals && tipAway == result.AwayGoals)
            return ScoreCategory.Exact;

        var tipDiff = tipHome - tipAway;
        var resultDiff = result.HomeGoals - result.AwayGoals;
        var tipOutcome = Math.Sign(tipDiff);
        var resultOutcome = Math.Sign(resultDiff);
        var sameOutcome = tipOutcome == resultOutcome;

        // Both 5- and 3-point categories require correct outcome.
        if (sameOutcome)
        {
            // Decisive matches: same winner + same goal difference → 5
            // (draw-vs-draw with different scores can't have same diff or it would be exact.)
            if (tipDiff != 0 && tipDiff == resultDiff)
                return ScoreCategory.WinnerAndGoalDiff;

            // Correct winner with wrong GD, OR correct draw with wrong score.
            return ScoreCategory.Winner;
        }

        // Wrong outcome: check the 1-point category — home matches home, OR away matches away.
        // Strictly NOT swapped per guide §3.
        if (tipHome == result.HomeGoals || tipAway == result.AwayGoals)
            return ScoreCategory.OneTeamGoals;

        return ScoreCategory.Nothing;
    }
}
