using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Admin;

namespace Tip4Gen.Api.Controllers;

public record LongTipOutcomesRequest(Guid? WinnerTeamId, string? TopScorerName, string? Reason);

public record LongTipOutcomesResponse(Guid? WinnerTeamId, string? WinnerTeamName, string? TopScorerName);

[ApiController]
[Route("api/admin/long-tips/outcomes")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class LongTipsAdminController(
    CurrentUserService currentUser,
    ILongTipOutcomesService outcomes) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var snapshot = await outcomes.GetAsync(ct);
        return snapshot is null
            ? Ok(new LongTipOutcomesResponse(null, null, null))
            : Ok(new LongTipOutcomesResponse(snapshot.WinnerTeamId, snapshot.WinnerTeamName, snapshot.TopScorerName));
    }

    [HttpPut]
    public async Task<IActionResult> Set([FromBody] LongTipOutcomesRequest body, CancellationToken ct)
    {
        var admin = await currentUser.GetOrCreateAsync(ct);
        var result = await outcomes.SetAsync(
            new SetOutcomesCommand(admin.Id, body.WinnerTeamId, body.TopScorerName, body.Reason), ct);

        return result switch
        {
            LongTipOutcomesResult.Success s => Ok(new LongTipOutcomesResponse(
                s.Snapshot.WinnerTeamId, s.Snapshot.WinnerTeamName, s.Snapshot.TopScorerName)),

            LongTipOutcomesResult.TournamentNotConfigured => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Tournament not configured",
                detail: "No tournament exists yet; seed fixtures first.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TournamentNotConfigured" }),

            LongTipOutcomesResult.WinnerTeamNotFound w => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Winner team not found",
                detail: $"No national team with id {w.TeamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "WinnerTeamNotFound" }),

            LongTipOutcomesResult.TopScorerNameTooLong t => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: "Top scorer name too long",
                detail: $"Name has {t.Length} characters; max {t.Max}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TopScorerNameTooLong" }),

            _ => throw new InvalidOperationException($"Unhandled LongTipOutcomesResult: {result.GetType().Name}"),
        };
    }
}
