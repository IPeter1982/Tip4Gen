using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController(AppDbContext db) : ControllerBase
{
    // Anonymous: <img src> tags can't carry Bearer tokens. User IDs are GUIDs and only
    // surface inside authenticated pages, so leakage risk is acceptable for a friends-
    // group app. The URL carries ?v={avatarVersion} so a fresh upload bypasses cache.
    [HttpGet("{userId:guid}/avatar")]
    [AllowAnonymous]
    public async Task<IActionResult> Avatar(Guid userId, CancellationToken ct)
    {
        var row = await db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Avatar, u.AvatarContentType, u.AvatarVersion })
            .FirstOrDefaultAsync(ct);
        if (row?.Avatar is null || row.AvatarContentType is null) return NotFound();

        Response.Headers.CacheControl = "public, max-age=86400, immutable";
        if (!string.IsNullOrEmpty(row.AvatarVersion))
            Response.Headers.ETag = $"\"{row.AvatarVersion}\"";
        return File(row.Avatar, row.AvatarContentType);
    }
}
