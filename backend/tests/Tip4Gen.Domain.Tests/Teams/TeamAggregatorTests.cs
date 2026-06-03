using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamAggregatorTests
{
    private static readonly Guid A = new("00000000-0000-0000-0000-00000000000a");
    private static readonly Guid B = new("00000000-0000-0000-0000-00000000000b");
    private static readonly Guid C = new("00000000-0000-0000-0000-00000000000c");

    private static TeamAggregator.MemberPoints PA(int p) => new(A, p);
    private static TeamAggregator.MemberPoints PB(int p) => new(B, p);
    private static TeamAggregator.MemberPoints PC(int p) => new(C, p);

    [Fact]
    public void Sums_all_three_member_points()
    {
        // §7 example reshaped for 3 members: András 7, Bea 10, RoboFoci 5 → total 22.
        var aggregate = TeamAggregator.ForMatch(new[] { PA(7), PB(10), PC(5) });
        Assert.Equal(22, aggregate.TotalPoints);
        Assert.Equal(3, aggregate.Members.Count);
        Assert.Equal(7, aggregate.Members.Single(m => m.MemberId == A).Points);
        Assert.Equal(10, aggregate.Members.Single(m => m.MemberId == B).Points);
        Assert.Equal(5, aggregate.Members.Single(m => m.MemberId == C).Points);
    }

    [Fact]
    public void All_zero_returns_zero_total()
    {
        var aggregate = TeamAggregator.ForMatch(new[] { PA(0), PB(0), PC(0) });
        Assert.Equal(0, aggregate.TotalPoints);
        Assert.All(aggregate.Members, m => Assert.Equal(0, m.Points));
    }

    [Fact]
    public void Member_with_zero_still_counts_no_drop()
    {
        // Previously the zero would be dropped; now it stays in the sum (= 18 not 18+0 dropped).
        var aggregate = TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(0) });
        Assert.Equal(18, aggregate.TotalPoints);
    }

    [Fact]
    public void Tied_members_all_contribute()
    {
        // No tiebreak logic anymore — every tied member's points are summed.
        var aggregate = TeamAggregator.ForMatch(new[] { PA(5), PB(5), PC(5) });
        Assert.Equal(15, aggregate.TotalPoints);
    }

    [Fact]
    public void Rejects_wrong_member_count()
    {
        Assert.Throws<ArgumentException>(() =>
            TeamAggregator.ForMatch(new[] { PA(10), PB(8) }));
        Assert.Throws<ArgumentException>(() =>
            TeamAggregator.ForMatch(new[] { PA(10), PB(8), PC(6), new TeamAggregator.MemberPoints(Guid.NewGuid(), 1) }));
    }

    [Fact]
    public void Rejects_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => TeamAggregator.ForMatch(null!));
    }
}
