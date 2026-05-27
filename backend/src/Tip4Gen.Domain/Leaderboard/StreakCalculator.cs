namespace Tip4Gen.Domain.Leaderboard;

/// <summary>
/// Computes the longest run of consecutive scored matches in which a player
/// earned at least <see cref="MinPointsPerMatch"/> points. Used as the §9
/// fourth-tier tiebreaker on the individual leaderboard.
///
/// The input must already be in chronological order; the calculator does no
/// sorting of its own. Only matches the player tipped on (and that have been
/// scored) belong in the sequence — matches with no tip / not yet finalized
/// are absences, not zero-point entries, and should be omitted entirely.
/// </summary>
public static class StreakCalculator
{
    public const int MinPointsPerMatch = 3;

    public static int LongestStreak(IEnumerable<int> chronologicalPointsPerMatch)
    {
        ArgumentNullException.ThrowIfNull(chronologicalPointsPerMatch);

        var longest = 0;
        var current = 0;
        foreach (var points in chronologicalPointsPerMatch)
        {
            if (points >= MinPointsPerMatch)
            {
                current++;
                if (current > longest) longest = current;
            }
            else
            {
                current = 0;
            }
        }
        return longest;
    }
}
