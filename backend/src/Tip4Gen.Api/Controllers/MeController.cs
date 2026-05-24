using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/me")]
[Authorize]
public class MeController(CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        return Ok(new
        {
            id = user.Id,
            displayName = user.DisplayName,
            auth0Sub = user.Auth0Sub,
            createdAt = user.CreatedAt,
            isAdmin = currentUser.IsAdmin,
        });
    }
}
