using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamLockPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 06, 11, 16, 00, 00, TimeSpan.Zero);

    [Fact]
    public void Skips_before_tournament_start()
    {
        var d = TeamLockPolicy.Decide(Start.AddMinutes(-1), Start, TeamStatus.Forming, 4);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Locks_at_exact_start_with_full_roster()
    {
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 4);
        Assert.Equal(TeamLockDecision.Lock, d);
    }

    [Fact]
    public void Disqualifies_at_exact_start_with_three_members()
    {
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 3);
        Assert.Equal(TeamLockDecision.Disqualify, d);
    }

    [Fact]
    public void Disqualifies_at_exact_start_with_one_member()
    {
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 1);
        Assert.Equal(TeamLockDecision.Disqualify, d);
    }

    [Fact]
    public void Skips_when_already_locked()
    {
        var d = TeamLockPolicy.Decide(Start.AddHours(1), Start, TeamStatus.Locked, 4);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Skips_when_already_disqualified()
    {
        var d = TeamLockPolicy.Decide(Start.AddHours(1), Start, TeamStatus.Disqualified, 2);
        Assert.Equal(TeamLockDecision.Skip, d);
    }
}
