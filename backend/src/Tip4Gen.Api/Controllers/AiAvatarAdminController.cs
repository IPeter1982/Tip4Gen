using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Api.Avatars;
using Tip4Gen.Domain.Users;
using Tip4Gen.Infrastructure.Settings;

namespace Tip4Gen.Api.Controllers;

[ApiController]
[Route("api/admin/ai-avatar")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AiAvatarAdminController(
    CurrentUserService currentUser,
    AiAvatarAdminService svc) : ControllerBase
{
    public record SetAiAvatarRequest(string DataUrl, string? Reason);

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetAiAvatarRequest body, CancellationToken ct)
    {
        if (!DataUrlParser.TryParse(body?.DataUrl, out var contentType, out var bytes))
            return Problem(
                title: "Invalid data URL",
                detail: "Hibás kép formátum.",
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["reason"] = nameof(UserRejectionReason.AvatarUnsupportedFormat) });

        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await svc.SetAsync(
            new SetAiAvatarCommand(admin.Id, bytes!, contentType!, body!.Reason), ct);

        return result switch
        {
            AiAvatarResult.Success s => Ok(new { aiAvatarVersion = s.Version }),
            AiAvatarResult.Rejected r => Problem(
                title: "Invalid avatar",
                detail: r.Message,
                statusCode: 400,
                extensions: new Dictionary<string, object?> { ["reason"] = r.Reason.ToString() }),
            _ => throw new InvalidOperationException($"Unhandled AiAvatarResult: {result.GetType().Name}"),
        };
    }

    [HttpDelete]
    public async Task<IActionResult> Clear([FromQuery] string? reason, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await svc.ClearAsync(admin.Id, reason, ct);
        return result switch
        {
            AiAvatarResult.Success s => Ok(new { aiAvatarVersion = s.Version }),
            _ => throw new InvalidOperationException($"Unhandled AiAvatarResult: {result.GetType().Name}"),
        };
    }
}
