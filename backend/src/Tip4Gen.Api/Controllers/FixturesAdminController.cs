using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Tournaments;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/admin/fixtures")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class FixturesAdminController(IFixtureSyncService sync) : ControllerBase
{
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken ct)
    {
        var result = await sync.SyncAsync(includeTeamsRoster: true, ct);
        return Ok(result);
    }
}
