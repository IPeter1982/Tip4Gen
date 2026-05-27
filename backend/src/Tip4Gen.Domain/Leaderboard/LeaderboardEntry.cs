namespace Tip4Gen.Domain.Leaderboard;

/// <summary>
/// One player's input to the ranker. Carries the four §9 tiebreaker signals
/// alongside the total points so the ranker doesn't need to peek into the DB.
///
/// Long-tip booleans are nullable to mean "outcome not yet known" — before the
/// tournament ends, no one has a correct/incorrect winner or top scorer. The
/// ranker treats null as a non-signal (neither breaks nor builds a tie) until
/// outcomes are recorded (Phase 8).
/// </summary>
public sealed record LeaderboardEntry(
    Guid UserId,
    string DisplayName,
    int TotalPoints,
    int ExactCount,
    bool? WinnerCorrect,
    bool? TopScorerCorrect,
    int LongestStreak);

public sealed record RankedLeaderboardEntry(
    int Rank,
    LeaderboardEntry Entry);
