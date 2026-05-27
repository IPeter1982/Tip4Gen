namespace Tip4Gen.Domain.Teams;

public enum TeamStatus
{
    /// <summary>Team is being assembled — members can join/leave, name + AI mode editable.</summary>
    Forming = 0,

    /// <summary>Locked at tournament-start kickoff; membership and AI mode are frozen.</summary>
    Locked = 1,

    /// <summary>Team did not reach 4 members by tournament start — out of team leaderboard.</summary>
    Disqualified = 2,
}
