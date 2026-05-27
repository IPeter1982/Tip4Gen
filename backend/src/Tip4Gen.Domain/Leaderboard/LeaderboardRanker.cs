namespace Tip4Gen.Domain.Leaderboard;

/// <summary>
/// Pure ranking + tiebreakers per guide §9. Standard competition ranking
/// ("1224"): tied entries share a rank and the next entry skips ahead.
///
/// Tiebreaker chain (descending priority):
///   1. TotalPoints
///   2. ExactCount (count of 10-pointers)
///   3. WinnerCorrect (true beats false; null means outcome not recorded yet — neutral)
///   4. TopScorerCorrect (same null semantics)
///   5. LongestStreak (consecutive matches with ≥3 points)
///   6. Fully tied → shared placement
///
/// Display order on full ties is secondary-sorted by DisplayName so the SPA shows
/// a deterministic list, but DisplayName is NOT part of the rank-tie detection.
/// </summary>
public static class LeaderboardRanker
{
    public static IReadOnlyList<RankedLeaderboardEntry> Rank(IEnumerable<LeaderboardEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var ordered = entries
            .OrderBy(e => e, Comparer<LeaderboardEntry>.Create(CompareForRank))
            .ThenBy(e => e.DisplayName, StringComparer.Ordinal)
            .ToList();

        var ranked = new List<RankedLeaderboardEntry>(ordered.Count);
        int rank = 0;
        LeaderboardEntry? prev = null;
        for (int i = 0; i < ordered.Count; i++)
        {
            var entry = ordered[i];
            if (prev is null || CompareForRank(prev, entry) != 0)
            {
                rank = i + 1;
            }
            ranked.Add(new RankedLeaderboardEntry(rank, entry));
            prev = entry;
        }
        return ranked;
    }

    private static int CompareForRank(LeaderboardEntry a, LeaderboardEntry b)
    {
        // All comparisons are descending: higher value sorts earlier.
        int c = b.TotalPoints.CompareTo(a.TotalPoints);
        if (c != 0) return c;

        c = b.ExactCount.CompareTo(a.ExactCount);
        if (c != 0) return c;

        c = CompareNullableBool(a.WinnerCorrect, b.WinnerCorrect);
        if (c != 0) return c;

        c = CompareNullableBool(a.TopScorerCorrect, b.TopScorerCorrect);
        if (c != 0) return c;

        return b.LongestStreak.CompareTo(a.LongestStreak);
    }

    private static int CompareNullableBool(bool? a, bool? b)
    {
        // Outcome unknown for at least one side → neutral, can't break a tie.
        if (a is null || b is null) return 0;
        return b.Value.CompareTo(a.Value); // true > false, descending
    }
}
