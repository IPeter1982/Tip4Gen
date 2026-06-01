using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/ai-avatar")]
public class AiAvatarController(AppDbContext db) : ControllerBase
{
    // Anonymous: <img src> tags can't carry Bearer tokens. The image is identical for
    // every authenticated user. URL carries ?v={version} so admin uploads bust cache.
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var row = await db.AiAvatarSettings.AsNoTracking()
            .Select(s => new { s.Avatar, s.ContentType, s.Version })
            .FirstOrDefaultAsync(ct);
        if (row is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400, immutable";
        if (!string.IsNullOrEmpty(row.Version))
            Response.Headers.ETag = $"\"{row.Version}\"";
        return File(row.Avatar, row.ContentType);
    }
}
