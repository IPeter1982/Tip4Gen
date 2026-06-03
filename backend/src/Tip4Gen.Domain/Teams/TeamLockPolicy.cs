namespace Tip4Gen.Domain.Teams;

public enum TeamLockDecision
{
    /// <summary>Leave the team Forming — either tournament hasn't started yet, or
    /// the roster is still under-sized and may grow.</summary>
    Skip,

    /// <summary>Lock the team — tournament has started and the roster is full.</summary>
    Lock,
}

/// <summary>
/// Pure decision rule for the auto-lock pass. A Forming team is locked once the tournament
/// has started AND the roster has reached <see cref="Team.MaxMembers"/>. Under-sized teams
/// stay Forming so members can still join (and new teams can be created) after the start —
/// they just don't appear on the team leaderboard until they're full.
/// </summary>
public static class TeamLockPolicy
{
    public static TeamLockDecision Decide(
        DateTimeOffset now,
        DateTimeOffset tournamentStartUtc,
        TeamStatus currentStatus,
        int memberCount)
    {
        if (currentStatus != TeamStatus.Forming) return TeamLockDecision.Skip;
        if (now < tournamentStartUtc) return TeamLockDecision.Skip;
        return memberCount >= Team.MaxMembers ? TeamLockDecision.Lock : TeamLockDecision.Skip;
    }
}
