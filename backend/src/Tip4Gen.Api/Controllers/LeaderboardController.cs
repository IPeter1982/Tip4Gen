using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Leaderboard;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/leaderboard")]
[Authorize]
public class LeaderboardController(
    CurrentUserService currentUser,
    IIndividualLeaderboardService individual,
    ITeamLeaderboardService teams) : ControllerBase
{
    [HttpGet("users")]
    public async Task<IActionResult> Users(CancellationToken ct)
    {
        var me = await currentUser.GetOrCreateAsync(ct);
        var rows = await individual.GetAsync(me.Id, ct);
        return Ok(rows);
    }

    [HttpGet("teams")]
    public async Task<IActionResult> Teams(CancellationToken ct)
    {
        var me = await currentUser.GetOrCreateAsync(ct);
        var rows = await teams.GetAsync(me.Id, ct);
        return Ok(rows);
    }
}
