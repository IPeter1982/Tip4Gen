namespace Tip4Gen.Domain.Teams;

/// <summary>
/// Pure "best 3 of 4" aggregation per guide §7. Given a Locked team's per-member
/// points on one match, decides which member is dropped and sums the rest.
///
/// Contract: caller passes exactly <see cref="Team.MaxMembers"/> entries. Members
/// with no tip (or AI fallback that hasn't fired yet) should be passed with Points=0
/// so they sort to the bottom naturally.
/// </summary>
public static class TeamAggregator
{
    public const int CountedScores = 3;

    public readonly record struct MemberPoints(Guid MemberId, int Points);

    public readonly record struct MemberAggregate(Guid MemberId, int Points, bool Dropped);

    public sealed record MatchAggregate(int TotalPoints, IReadOnlyList<MemberAggregate> Members);

    /// <summary>
    /// Builds the breakdown. On ties for the lowest score, drops the member whose
    /// id sorts highest — deterministic, and rotates the "dropped" slot somewhat
    /// fairly across all-tied matches without affecting the team total.
    /// </summary>
    public static MatchAggregate ForMatch(IReadOnlyList<MemberPoints> members)
    {
        ArgumentNullException.ThrowIfNull(members);
        if (members.Count != Team.MaxMembers)
            throw new ArgumentException(
                $"Aggregator expects exactly {Team.MaxMembers} members; got {members.Count}.",
                nameof(members));

        // Find the index to drop: lowest Points, tiebreak by largest MemberId.
        int dropIndex = 0;
        for (int i = 1; i < members.Count; i++)
        {
            var candidate = members[i];
            var current = members[dropIndex];
            if (candidate.Points < current.Points)
                dropIndex = i;
            else if (candidate.Points == current.Points && candidate.MemberId.CompareTo(current.MemberId) > 0)
                dropIndex = i;
        }

        var aggregates = new MemberAggregate[members.Count];
        int total = 0;
        for (int i = 0; i < members.Count; i++)
        {
            var dropped = i == dropIndex;
            aggregates[i] = new MemberAggregate(members[i].MemberId, members[i].Points, dropped);
            if (!dropped) total += members[i].Points;
        }

        return new MatchAggregate(total, aggregates);
    }
}
