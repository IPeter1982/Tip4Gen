using Tip4Gen.Domain.Leaderboard;

namespace Tip4Gen.Domain.Tests.Leaderboard;

public class LeaderboardRankerTests
{
    private static LeaderboardEntry E(
        string name,
        int total,
        int exact = 0,
        bool? winner = null,
        bool? topScorer = null,
        int streak = 0)
        => new(Guid.NewGuid(), name, total, exact, winner, topScorer, streak);

    [Fact]
    public void Empty_input_returns_empty_list()
    {
        Assert.Empty(LeaderboardRanker.Rank(Array.Empty<LeaderboardEntry>()));
    }

    [Fact]
    public void Sorts_descending_by_total_points()
    {
        var ranked = LeaderboardRanker.Rank(new[] { E("a", 10), E("b", 30), E("c", 20) });
        Assert.Equal(new[] { 30, 20, 10 }, ranked.Select(r => r.Entry.TotalPoints));
        Assert.Equal(new[] { 1, 2, 3 }, ranked.Select(r => r.Rank));
    }

    [Fact]
    public void Exact_count_breaks_a_points_tie()
    {
        // both 50 pts, but B has more exact tips
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 2),
            E("B", 50, exact: 5),
        });
        Assert.Equal("B", ranked[0].Entry.DisplayName);
        Assert.Equal("A", ranked[1].Entry.DisplayName);
        Assert.Equal(new[] { 1, 2 }, ranked.Select(r => r.Rank));
    }

    [Fact]
    public void Winner_correct_breaks_when_exact_count_ties()
    {
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 3, winner: false),
            E("B", 50, exact: 3, winner: true),
        });
        Assert.Equal("B", ranked[0].Entry.DisplayName);
    }

    [Fact]
    public void Top_scorer_breaks_when_winner_also_ties()
    {
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 3, winner: true, topScorer: false),
            E("B", 50, exact: 3, winner: true, topScorer: true),
        });
        Assert.Equal("B", ranked[0].Entry.DisplayName);
    }

    [Fact]
    public void Streak_is_the_last_resort_tiebreaker()
    {
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 3, winner: true, topScorer: false, streak: 4),
            E("B", 50, exact: 3, winner: true, topScorer: false, streak: 6),
        });
        Assert.Equal("B", ranked[0].Entry.DisplayName);
    }

    [Fact]
    public void Null_long_tip_outcome_is_neutral_does_not_break_tie()
    {
        // Same points + exact, neither has outcomes recorded → fully tied → shared rank
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 3, winner: null, topScorer: null),
            E("B", 50, exact: 3, winner: null, topScorer: null),
        });
        Assert.All(ranked, r => Assert.Equal(1, r.Rank));
    }

    [Fact]
    public void Null_on_one_side_does_not_outrank_known_false()
    {
        // A has WinnerCorrect=false, B is null (outcome unknown for B somehow).
        // Per §9, null is neutral so the tiebreaker is unresolved at this level.
        // They should fall through to the next tiebreaker (streak).
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("A", 50, exact: 3, winner: false, streak: 5),
            E("B", 50, exact: 3, winner: null,  streak: 2),
        });
        Assert.Equal("A", ranked[0].Entry.DisplayName); // streak breaks
        Assert.Equal(new[] { 1, 2 }, ranked.Select(r => r.Rank));
    }

    [Fact]
    public void Fully_tied_entries_share_rank_and_next_rank_skips()
    {
        // Standard competition ranking: 1, 2, 2, 4 (not 1, 2, 2, 3)
        var ranked = LeaderboardRanker.Rank(new[]
        {
            E("Alice", 100),
            E("Bob",   90),
            E("Carl",  90),
            E("Dora",  80),
        });
        Assert.Equal(new[] { 1, 2, 2, 4 }, ranked.Select(r => r.Rank));
    }

    [Fact]
    public void Ties_are_secondary_sorted_by_display_name_for_determinism()
    {
        var ranked = LeaderboardRanker.Rank(new[] { E("Zed", 50), E("Ann", 50), E("Mia", 50) });
        Assert.Equal(new[] { "Ann", "Mia", "Zed" }, ranked.Select(r => r.Entry.DisplayName));
        Assert.All(ranked, r => Assert.Equal(1, r.Rank)); // all share rank 1
    }

    [Fact]
    public void Rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => LeaderboardRanker.Rank(null!));
    }
}
