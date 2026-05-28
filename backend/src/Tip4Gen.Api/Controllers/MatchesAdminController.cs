using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Tournaments;
using Tip4Gen.Infrastructure.Admin;

namespace Tip4Gen.Api.Controllers;

public record MatchResultRequest(int HomeGoals, int AwayGoals, MatchStatus Status, string? Reason);
public record MatchCancelRequest(string? Reason);
public record MatchPostponeRequest(DateTimeOffset NewKickoffUtc, string? Reason);
public record MatchCancelResponse(Guid MatchId, int ScoredTipsCleared, int JokersRefunded);
public record MatchPostponeResponse(Guid MatchId, DateTimeOffset NewKickoffUtc, DateTimeOffset NewDeadlineUtc);

[ApiController]
[Route("api/admin/matches")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class MatchesAdminController(
    CurrentUserService currentUser,
    IMatchAdminService matchAdmin) : ControllerBase
{
    /// <summary>
    /// Set or correct the final score for a match. Status must be Finished (normal
    /// outcome) or Awarded (FIFA-decided per guide §11). Triggers idempotent re-scoring
    /// inside the same request so the response reflects updated totals.
    /// </summary>
    [HttpPut("{matchId:guid}/result")]
    public async Task<IActionResult> SetResult(Guid matchId, [FromBody] MatchResultRequest body, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var cmd = new SetResultCommand(
            AdminUserId: admin.Id,
            MatchId: matchId,
            HomeGoals: body.HomeGoals,
            AwayGoals: body.AwayGoals,
            NewStatus: body.Status,
            Reason: body.Reason);

        var result = await matchAdmin.SetResultAsync(cmd, ct);
        return result switch
        {
            MatchAdminResult.Success s => Ok(new RescoreResponse(s.MatchId, s.TipsScored, s.TotalPoints)),

            MatchAdminResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "MatchNotFound" }),

            MatchAdminResult.InvalidScore => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Invalid score",
                detail: "Goals must be integers in 0–15.",
                extensions: new Dictionary<string, object?> { ["reason"] = "InvalidScore" }),

            MatchAdminResult.InvalidStatusRequested isr => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Invalid status",
                detail: $"Status must be Finished or Awarded (got {isr.Requested}).",
                extensions: new Dictionary<string, object?> { ["reason"] = "InvalidStatusRequested" }),

            _ => throw new InvalidOperationException($"Unhandled MatchAdminResult: {result.GetType().Name}"),
        };
    }

    /// <summary>
    /// Cancel a match per guide §11. Clears recorded score, sets status Cancelled,
    /// deletes scored_tips for the match, and refunds the joker on every tip that
    /// played one. Tip rows remain as a historical record.
    /// </summary>
    [HttpPost("{matchId:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid matchId, [FromBody] MatchCancelRequest body, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await matchAdmin.CancelAsync(
            new CancelMatchCommand(admin.Id, matchId, body.Reason), ct);

        return result switch
        {
            MatchCancelResult.Success s => Ok(new MatchCancelResponse(s.MatchId, s.ScoredTipsCleared, s.JokersRefunded)),

            MatchCancelResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "MatchNotFound" }),

            MatchCancelResult.AlreadyCancelled => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Already cancelled",
                detail: "This match has already been cancelled.",
                extensions: new Dictionary<string, object?> { ["reason"] = "AlreadyCancelled" }),

            _ => throw new InvalidOperationException($"Unhandled MatchCancelResult: {result.GetType().Name}"),
        };
    }

    /// <summary>
    /// Postpone a match. Sets a new kickoff (and implicit -1h deadline) and flips
    /// status to Postponed. Existing tips carry over and remain editable until the
    /// new deadline. Jokers are NOT refunded.
    /// </summary>
    [HttpPost("{matchId:guid}/postpone")]
    public async Task<IActionResult> Postpone(Guid matchId, [FromBody] MatchPostponeRequest body, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await matchAdmin.PostponeAsync(
            new PostponeMatchCommand(admin.Id, matchId, body.NewKickoffUtc, body.Reason), ct);

        return result switch
        {
            MatchPostponeResult.Success s => Ok(new MatchPostponeResponse(s.MatchId, s.NewKickoffUtc, s.NewDeadlineUtc)),

            MatchPostponeResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "MatchNotFound" }),

            MatchPostponeResult.InvalidStatus inv => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Cannot postpone",
                detail: $"Match status {inv.Current} doesn't allow postponement (only Scheduled or already Postponed).",
                extensions: new Dictionary<string, object?> { ["reason"] = "InvalidStatus", ["status"] = inv.Current.ToString() }),

            MatchPostponeResult.KickoffNotFarEnough k => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Kickoff too soon",
                detail: $"New kickoff must be at least 1h + buffer in the future. Earliest allowed: {k.MinAllowed:O}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "KickoffNotFarEnough" }),

            _ => throw new InvalidOperationException($"Unhandled MatchPostponeResult: {result.GetType().Name}"),
        };
    }
}
