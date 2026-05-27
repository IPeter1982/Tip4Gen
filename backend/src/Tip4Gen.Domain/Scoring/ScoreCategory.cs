namespace Tip4Gen.Domain.Scoring;

/// <summary>
/// Per-match scoring categories per guide §3. Highest-value match wins;
/// the integer value is the base points the category awards.
/// </summary>
public enum ScoreCategory
{
    /// <summary>Nothing matches: 0 points.</summary>
    Nothing = 0,

    /// <summary>One team's goal count matches (home-to-home OR away-to-away, never swapped): 1 point.</summary>
    OneTeamGoals = 1,

    /// <summary>Correct winner OR correct draw with wrong scoreline: 3 points.</summary>
    Winner = 3,

    /// <summary>Correct winner AND correct goal difference, but not exact: 5 points.</summary>
    WinnerAndGoalDiff = 5,

    /// <summary>Exact score: 10 points.</summary>
    Exact = 10,
}
