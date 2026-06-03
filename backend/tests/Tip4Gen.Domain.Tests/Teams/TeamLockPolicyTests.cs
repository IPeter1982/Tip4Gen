using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamLockPolicyTests
{
    private static readonly DateTimeOffset Start = new(2026, 06, 11, 16, 00, 00, TimeSpan.Zero);

    [Fact]
    public void Skips_before_tournament_start_even_when_full()
    {
        var d = TeamLockPolicy.Decide(Start.AddMinutes(-1), Start, TeamStatus.Forming, 3);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Locks_at_exact_start_with_full_roster()
    {
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 3);
        Assert.Equal(TeamLockDecision.Lock, d);
    }

    [Fact]
    public void Skips_at_exact_start_with_two_members_keeping_team_open()
    {
        // Under-sized teams stay Forming so members can still join after tournament start.
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 2);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Skips_at_exact_start_with_one_member()
    {
        var d = TeamLockPolicy.Decide(Start, Start, TeamStatus.Forming, 1);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Locks_late_when_third_member_joins_after_start()
    {
        // A team that grew to full capacity *after* tournament start should lock on the
        // next job tick — that's the late-join path the new rules enable.
        var d = TeamLockPolicy.Decide(Start.AddHours(2), Start, TeamStatus.Forming, 3);
        Assert.Equal(TeamLockDecision.Lock, d);
    }

    [Fact]
    public void Skips_when_already_locked()
    {
        var d = TeamLockPolicy.Decide(Start.AddHours(1), Start, TeamStatus.Locked, 3);
        Assert.Equal(TeamLockDecision.Skip, d);
    }

    [Fact]
    public void Skips_when_already_disqualified()
    {
        var d = TeamLockPolicy.Decide(Start.AddHours(1), Start, TeamStatus.Disqualified, 2);
        Assert.Equal(TeamLockDecision.Skip, d);
    }
}
