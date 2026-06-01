using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Users;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        return Ok(MePayload(user));
    }

    public record UpdateMeRequest(string DisplayName);

    [HttpPatch]
    public async Task<IActionResult> Patch([FromBody] UpdateMeRequest body, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var validation = UserRulesValidator.ValidateDisplayName(body?.DisplayName);
        if (!validation.IsValid)
            return Problem(
                title: "Invalid display name",
                detail: validation.Message,
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["reason"] = validation.Reason.ToString() });

        user.Rename(body!.DisplayName.Trim());
        await db.SaveChangesAsync(ct);
        return Ok(MePayload(user));
    }

    private object MePayload(Domain.Users.User user) => new
    {
        id = user.Id,
        displayName = user.DisplayName,
        auth0Sub = user.Auth0Sub,
        createdAt = user.CreatedAt,
        isAdmin = currentUser.IsAdmin,
    };
}
