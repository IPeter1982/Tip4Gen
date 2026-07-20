namespace Tip4Gen.Domain.Scoring;

/// <summary>
/// Per guide §5: correctly guessing the tournament winner is worth 50 points,
/// correctly guessing the top scorer 30. §7 clarifies these flow into the team
/// total per-human-member (AI members can't tip long-term, so contribute 0).
///
/// Null correctness → no bonus (outcome not yet recorded, same neutral semantics
/// the leaderboard ranker uses for §9 tiebreakers).
/// </summary>
public static class LongTipBonus
{
    public const int WinnerPoints = 50;
    public const int TopScorerPoints = 30;

    public static int Compute(bool? winnerCorrect, bool? topScorerCorrect)
    {
        int bonus = 0;
        if (winnerCorrect == true) bonus += WinnerPoints;
        if (topScorerCorrect == true) bonus += TopScorerPoints;
        return bonus;
    }
}
