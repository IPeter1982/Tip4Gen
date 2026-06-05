using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

public record PlayerListItem(Guid Id, string Name, Guid TeamId, string TeamName, string? TeamCode);

[ApiController]
[Route("api/players")]
[Authorize]
public class PlayersController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct)
    {
        var rows = await (
            from p in db.Players.AsNoTracking()
            join n in db.NationalTeams.AsNoTracking() on p.NationalTeamId equals n.Id
            orderby n.Name, p.Name
            select new PlayerListItem(p.Id, p.Name, n.Id, n.Name, n.Code)
        ).ToListAsync(ct);
        return Ok(rows);
    }
}
