using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;

namespace Tip4Gen.Api.Controllers;

public record AiTipperPreviewRequest(
    string HomeTeam,
    string AwayTeam,
    Stage Stage,
    AiMode Mode);

public record AiTipperPreviewResponse(
    string Outcome,
    AiTipResponse? Tip,
    string? Error,
    string? RawText);

[ApiController]
[Route("api/admin/ai-tipper")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AiTipperAdminController(IAiTipper tipper) : ControllerBase
{
    /// <summary>
    /// One-shot preview: call the configured AI tipper for a synthetic match and return
    /// the raw result. Useful for verifying the API key + JSON-mode plumbing without
    /// having to stage a near-future Scheduled match against a Locked team. Does not
    /// touch the database.
    /// </summary>
    [HttpPost("preview")]
    public async Task<IActionResult> Preview([FromBody] AiTipperPreviewRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.HomeTeam) || string.IsNullOrWhiteSpace(request.AwayTeam))
        {
            return Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Invalid request",
                detail: "homeTeam and awayTeam are required.");
        }

        var result = await tipper.GenerateAsync(
            request.HomeTeam.Trim(),
            request.AwayTeam.Trim(),
            request.Stage,
            request.Mode,
            ct);

        return Ok(result switch
        {
            AiTipResult.Success s => new AiTipperPreviewResponse("Success", s.Response, null, null),
            AiTipResult.Disabled => new AiTipperPreviewResponse("Disabled", null, "OpenAi:ApiKey is not configured", null),
            AiTipResult.Timeout => new AiTipperPreviewResponse("Timeout", null, "OpenAI did not respond in time", null),
            AiTipResult.ProviderError pe => new AiTipperPreviewResponse("ProviderError", null, pe.Error, null),
            AiTipResult.InvalidResponse ir => new AiTipperPreviewResponse("InvalidResponse", null, ir.Error, ir.RawText),
            _ => new AiTipperPreviewResponse(result.GetType().Name, null, "unknown result", null),
        });
    }
}
