using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Ai;
using Tip4Gen.Domain.Teams;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Ai;

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

public record AiTipperManualRunRequest(string? Reason);

public record AiTipperManualRunResponse(
    int AiMembers,
    int Attempted,
    int Written,
    int Fallbacks,
    int Skipped);

[ApiController]
[Route("api/admin/ai-tipper")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class AiTipperAdminController(
    IAiTipper tipper,
    IAiTippingService aiTipping,
    CurrentUserService currentUser) : ControllerBase
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

    /// <summary>
    /// Force the AI tippers to submit immediately for one match, bypassing the schedule
    /// policy. Every Locked team's AI slot that doesn't yet have a tip gets one — AI on
    /// success, deterministic 1–1 fallback on AI failure. One <c>admin_audit</c> row per
    /// call. Useful when the auto-job's [T-2h, T-1h] window doesn't suit (e.g. testing,
    /// or recovering from a stretched OpenAI outage).
    /// </summary>
    [HttpPost("run/{matchId:guid}")]
    public async Task<IActionResult> Run(
        Guid matchId,
        [FromBody] AiTipperManualRunRequest body,
        CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await aiTipping.RunForMatchAsync(matchId, admin.Id, body?.Reason, ct);

        return result switch
        {
            AiTipperManualRunResult.Success s => Ok(new AiTipperManualRunResponse(
                s.AiMembers, s.Attempted, s.Written, s.Fallbacks, s.Skipped)),

            AiTipperManualRunResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "MatchNotFound" }),

            AiTipperManualRunResult.MatchNotEligible mne => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Match not eligible",
                detail: $"Match must be Scheduled and kickoff in the future (got status={mne.Status}, kickoff={mne.KickoffUtc:O}).",
                extensions: new Dictionary<string, object?>
                {
                    ["reason"] = "MatchNotEligible",
                    ["status"] = mne.Status.ToString(),
                }),

            _ => throw new InvalidOperationException($"Unhandled AiTipperManualRunResult: {result.GetType().Name}"),
        };
    }
}
