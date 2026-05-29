using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Notifications;
using Tip4Gen.Infrastructure.Persistence;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/me/preferences")]
[Authorize]
public class PreferencesController(AppDbContext db, CurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var prefs = await db.UserPreferences.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        // Default: enabled. Matches the column default + the worker's fallback when no row exists.
        var enabled = prefs?.EmailRemindersEnabled ?? true;
        return Ok(new
        {
            emailRemindersEnabled = enabled,
            hasEmail = !string.IsNullOrEmpty(user.Email),
        });
    }

    public record UpdatePreferencesRequest(bool EmailRemindersEnabled);

    [HttpPut]
    public async Task<IActionResult> Put([FromBody] UpdatePreferencesRequest body, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var prefs = await db.UserPreferences.FirstOrDefaultAsync(p => p.UserId == user.Id, ct);
        if (prefs is null)
        {
            prefs = new UserPreferences(user.Id, body.EmailRemindersEnabled);
            db.UserPreferences.Add(prefs);
        }
        else
        {
            prefs.SetEmailReminders(body.EmailRemindersEnabled);
        }
        await db.SaveChangesAsync(ct);

        return Ok(new
        {
            emailRemindersEnabled = prefs.EmailRemindersEnabled,
            hasEmail = !string.IsNullOrEmpty(user.Email),
        });
    }
}
