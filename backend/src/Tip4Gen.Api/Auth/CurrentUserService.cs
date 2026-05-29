using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Domain.Users;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Auth;

public class CurrentUserService(AppDbContext db, IHttpContextAccessor httpContextAccessor, Auth0Options auth0)
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
            // so the notifications worker (Phase 9) always has a current address. SetEmail
            // no-ops when nothing changed, so the SaveChanges only fires on actual drift.
            user.SetEmail(emailClaim);
            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(ct);
            return user;
        }

        var displayName =
            httpContextAccessor.HttpContext?.User?.FindFirstValue("name")
            ?? httpContextAccessor.HttpContext?.User?.FindFirstValue("nickname")
            ?? emailClaim
            ?? sub;

        user = new User(sub, displayName, emailClaim);
        db.Users.Add(user);
        await db.SaveChangesAsync(ct);
        return user;
    }
}
