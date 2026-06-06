using Tip4Gen.Domain.Teams;

namespace Tip4Gen.Domain.Tests.Teams;

public class TeamTests
{
    [Fact]
    public void Lock_flips_status_and_bumps_updated_at()
    {
        var team = new Team("Alpha");
        var before = team.UpdatedAt;
        Thread.Sleep(2);

        team.Lock();

        Assert.Equal(TeamStatus.Locked, team.Status);
        Assert.True(team.UpdatedAt > before);
    }

    [Fact]
    public void Unlock_flips_status_back_to_forming_and_bumps_updated_at()
    {
        var team = new Team("Alpha");
        team.Lock();
        var before = team.UpdatedAt;
        Thread.Sleep(2);

        team.Unlock();

        Assert.Equal(TeamStatus.Forming, team.Status);
        Assert.True(team.UpdatedAt > before);
    }

    [Fact]
    public void Unlock_from_forming_is_idempotent()
    {
        var team = new Team("Alpha");
        Assert.Equal(TeamStatus.Forming, team.Status);

        team.Unlock();

        Assert.Equal(TeamStatus.Forming, team.Status);
    }
}
