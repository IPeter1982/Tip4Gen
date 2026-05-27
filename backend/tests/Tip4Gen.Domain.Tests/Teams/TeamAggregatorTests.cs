using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamAggregatorTests
{
    private static readonly Guid A = new("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = new("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = new("00000000-0000-0000-0000-00000000000c");
    private static readonly Guid D = new("00000000-0000-0000-0000-00000000000d");

    private static TeamAggregator.MemberPoints PA(int p) => new(A, p);
    private static TeamAggregator.MemberPoints PB(int p) => new(B, p);
    private static TeamAggregator.MemberPoints PC(int p) => new(C, p);
    private static TeamAggregator.MemberPoints PD(int p) => new(D, p);

    [Fact]
    public void Guide_example_22_points_drops_dani()
    {
        // §7 example: András 7, Bea 10, RoboFoci 5, Dani 0 → total 22, Dani dropped
        var aggregate = TeamAggregator.ForMatch(new[] { PA(7), PB(10), PC(5), PD(0) });
        Assert.Equal(22, aggregate.TotalPoints);
        Assert.True(aggregate.Members.Single(m => m.MemberId == D).Dropped);
        Assert.False(aggregate.Members.Single(m => m.MemberId == A).Dropped);
    }

    [Fact]
    public void Drops_lowest_when_clearly_distinct()
    {
        var aggregate = TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(6), PD(4) });
        Assert.Equal(24, aggregate.TotalPoints);
        Assert.True(aggregate.Members.Single(m => m.MemberId == D).Dropped);
    }

    [Fact]
    public void All_zero_still_returns_zero_total_with_one_dropped()
    {
        var aggregate = TeamAggregator.ForMatch(new[] { PA(0), PB(0), PC(0), PD(0) });
        Assert.Equal(0, aggregate.TotalPoints);
        Assert.Equal(1, aggregate.Members.Count(m => m.Dropped));
    }

    [Fact]
    public void Tie_on_lowest_drops_largest_member_id_for_determinism()
    {
        // A=5 B=5 C=5 D=5 — all tied for lowest. Tiebreak picks the highest-sorted id (D).
        var aggregate = TeamAggregator.ForMatch(new[] { PA(5), PB(5), PC(5), PD(5) });
        Assert.Equal(15, aggregate.TotalPoints);
        Assert.True(aggregate.Members.Single(m => m.MemberId == D).Dropped);
    }

    [Fact]
    public void Tie_only_among_lowest_pair_is_resolved_deterministically()
    {
        // C and D tied at 3 → drop D.
        var aggregate = TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(3), PD(3) });
        Assert.Equal(21, aggregate.TotalPoints);
        Assert.True(aggregate.Members.Single(m => m.MemberId == D).Dropped);
    }

    [Fact]
    public void High_scorers_never_dropped()
    {
        var aggregate = TeamAggregator.ForMatch(new[] { PA(30), PB(30), PC(30), PD(0) });
        Assert.Equal(90, aggregate.TotalPoints);
        Assert.True(aggregate.Members.Single(m => m.MemberId == D).Dropped);
    }

    [Fact]
    public void Rejects_wrong_member_count()
    {
        Assert.Throws<ArgumentException>(() =>
            TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(6) }));
        Assert.Throws<ArgumentException>(() =>
            TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(6), PD(4), new TeamAggregator.MemberPoints(Guid.NewGuid(), 1) }));
    }

    [Fact]
    public void Rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => TeamAggregator.ForMatch(null!));
    }
}
