namespace Tip4Gen.Domain.Teams;

public enum TeamLockDecision
{
    /// <summary>Too early — tournament hasn't started yet, leave the team Forming.</summary>
    Skip,

    /// <summary>Lock the team — it had a full roster at tournament start.</summary>
    Lock,

    /// <summary>Disqualify the team — under-sized at tournament start, drops out of team leaderboard.</summary>
    Disqualify,
}

/// <summary>
/// Pure decision rule for the tournament-start team-lock pass. Kept separate from the
/// service so the timing/threshold logic is testable without a DbContext.
/// </summary>
public static class TeamLockPolicy
{
    public static TeamLockDecision Decide(
        DateTimeOffset now,
        DateTimeOffset tournamentStartUtc,
        TeamStatus currentStatus,
        int memberCount)
    {
        // Only Forming teams are candidates; Locked / Disqualified are already terminal.
        if (currentStatus != TeamStatus.Forming) return TeamLockDecision.Skip;
        if (now < tournamentStartUtc) return TeamLockDecision.Skip;
        return memberCount >= Team.MaxMembers ? TeamLockDecision.Lock : TeamLockDecision.Disqualify;
    }
}
