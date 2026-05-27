using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

public record NationalTeamResponse(Guid Id, string Name, string? Code);

[ApiController]
[Route("api/national-teams")]
[Authorize]
public class NationalTeamsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var teams = await db.NationalTeams.AsNoTracking()
            .OrderBy(t => t.Name)
            .Select(t => new NationalTeamResponse(t.Id, t.Name, t.Code))
            .ToListAsync(ct);
        return Ok(teams);
    }
}
