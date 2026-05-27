using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Teams;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/admin/teams")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class TeamsAdminController(ITeamLockService teamLock) : ControllerBase
{
    /// <summary>
    /// Manual trigger for the tournament-start lock pass. Useful when the Workers
    /// process isn't running, or to force-flip Forming teams immediately.
    /// Idempotent — re-running after a successful pass returns zeros.
    /// </summary>
    [HttpPost("lock")]
    public async Task<IActionResult> Lock(CancellationToken ct)
    {
        var summary = await teamLock.LockAllAsync(ct);
        return Ok(summary);
    }
}
