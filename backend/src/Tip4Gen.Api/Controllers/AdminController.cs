using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AdminController(CurrentUserService currentUser) : ControllerBase
{
    [HttpGet("me")]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        return Ok(new
        {
            id = user.Id,
            displayName = user.DisplayName,
            auth0Sub = user.Auth0Sub,
            role = "admin",
        });
    }
}
