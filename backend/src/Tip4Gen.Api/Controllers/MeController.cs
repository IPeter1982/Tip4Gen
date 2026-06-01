using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tip4Gen.Api.Auth;
using Tip4Gen.Api.Avatars;
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
        return Ok(await MePayloadAsync(user, ct));
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
        return Ok(await MePayloadAsync(user, ct));
    }

    public record SetAvatarRequest(string DataUrl);

    [HttpPut("avatar")]
    public async Task<IActionResult> SetAvatar([FromBody] SetAvatarRequest body, CancellationToken ct)
    {
        if (!DataUrlParser.TryParse(body?.DataUrl, out var contentType, out var bytes))
            return Problem(
                title: "Invalid data URL",
                detail: "Hibás kép formátum.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["reason"] = nameof(UserRejectionReason.AvatarUnsupportedFormat) });

        var validation = UserRulesValidator.ValidateAvatar(bytes, contentType);
        if (!validation.IsValid)
            return Problem(
                title: "Invalid avatar",
                detail: validation.Message,
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["reason"] = validation.Reason.ToString() });

        var user = await currentUser.GetOrCreateAsync(ct);
        var version = Convert.ToHexString(SHA256.HashData(bytes!))[..8].ToLowerInvariant();
        user.SetAvatar(bytes!, contentType!, version);
        await db.SaveChangesAsync(ct);
        return Ok(await MePayloadAsync(user, ct));
    }

    [HttpDelete("avatar")]
    public async Task<IActionResult> DeleteAvatar(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        user.ClearAvatar();
        await db.SaveChangesAsync(ct);
        return Ok(await MePayloadAsync(user, ct));
    }

    private async Task<object> MePayloadAsync(Domain.Users.User user, CancellationToken ct)
    {
        var aiAvatarVersion = await db.AiAvatarSettings.AsNoTracking()
            .Select(s => (string?)s.Version)
            .FirstOrDefaultAsync(ct);
        return new
        {
            id = user.Id,
            displayName = user.DisplayName,
            auth0Sub = user.Auth0Sub,
            createdAt = user.CreatedAt,
            isAdmin = currentUser.IsAdmin,
            avatarVersion = user.AvatarVersion,
            aiAvatarVersion,
        };
    }
}
