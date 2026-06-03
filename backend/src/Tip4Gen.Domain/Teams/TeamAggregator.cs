namespace Tip4Gen.Domain.Teams;

/// <summary>
/// Pure per-match team aggregation per guide §7. Sums all members' points for one
/// match — every member counts, no dropping.
///
/// Contract: caller passes exactly <see cref="Team.MaxMembers"/> entries. Members
/// with no tip (or AI fallback that hasn't fired yet) should be passed with Points=0
/// so they contribute zero to the total.
/// </summary>
public static class TeamAggregator
{
    public readonly record struct MemberPoints(Guid MemberId, int Points);

    public readonly record struct MemberAggregate(Guid MemberId, int Points);

    public sealed record MatchAggregate(int TotalPoints, IReadOnlyList<MemberAggregate> Members);

    public static MatchAggregate ForMatch(IReadOnlyList<MemberPoints> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count != Team.MaxMembers)
            throw new ArgumentException(
                $"Aggregator expects exactly {Team.MaxMembers} members; got {members.Count}.",
                nameof(members));

        var aggregates = new MemberAggregate[members.Count];
        int total = 0;
        for (int i = 0; i < members.Count; i++)
        {
            aggregates[i] = new MemberAggregate(members[i].MemberId, members[i].Points);
            total += members[i].Points;
        }

        return new MatchAggregate(total, aggregates);
    }
}
