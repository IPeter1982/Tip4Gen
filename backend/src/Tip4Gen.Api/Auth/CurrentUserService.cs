using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Users;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Auth;

public class CurrentUserService(
    AppDbContext db,
    IHttpContextAccessor httpContextAccessor,
    Auth0Options auth0,
    IAuth0UserInfoClient userInfo)
{
    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public string? Auth0Sub =>
        httpContextAccessor.HttpContext?.User?.FindFirstValue("sub")
        ?? httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAdmin =>
        IsAuthenticated
        && !string.IsNullOrWhiteSpace(auth0.AdminSub)
        && Auth0Sub == auth0.AdminSub;

    public async Task<User> GetOrCreateAsync(CancellationToken ct = default)
    {
        var sub = Auth0Sub ?? throw new InvalidOperationException("No 'sub' claim on authenticated request");
        var emailClaim = httpContextAccessor.HttpContext?.User?.FindFirstValue("email");

        var user = await db.Users.FirstOrDefaultAsync(u => u.Auth0Sub == sub, ct);
        if (user is not null)
        {
            // Keep the email column in sync with the claim on every authenticated request
            // so the notifications worker (Phase 9) always has a current address. If the
            // claim isn't on the access token (Auth0 default — `email` lives on the ID
            // token / userinfo) we fall through to a one-shot /userinfo call below.
            user.SetEmail(emailClaim);
            if (string.IsNullOrEmpty(user.Email))
            {
                var fetched = await TryFetchUserInfoEmailAsync(ct);
                if (fetched is not null) user.SetEmail(fetched);
            }
            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(ct);
            return user;
        }

        var displayName =
            httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("nickname")
            ?? emailClaim
            ?? sub;

        // First-time login: try the userinfo lookup so the row lands with an email already.
        var initialEmail = emailClaim ?? await TryFetchUserInfoEmailAsync(ct);
        user = new User(sub, displayName, initialEmail);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }

    private async Task<string?> TryFetchUserInfoEmailAsync(CancellationToken ct)
    {
        var raw = httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(raw) || !raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;
        var token = raw["Bearer ".Length..].Trim();
        if (token.Length == 0) return null;
        return await userInfo.FetchEmailAsync(token, ct);
    }
}
