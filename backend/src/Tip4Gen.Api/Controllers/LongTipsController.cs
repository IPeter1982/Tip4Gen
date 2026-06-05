using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Infrastructure.Tipping;

namespace Tip4Gen.Api.Controllers;

public record LongTipsRequest(Guid? WinnerTeamId, Guid? TopScorerPlayerId);

public record LongTipsResponse(
    Guid? WinnerTeamId,
    string? WinnerTeamName,
    Guid? TopScorerPlayerId,
    string? TopScorerPlayerName,
    string? TopScorerTeamCode,
    DateTimeOffset? WinnerSubmittedAt,
    DateTimeOffset? TopScorerSubmittedAt,
    DateTimeOffset LockUtc,
    bool Locked);

[ApiController]
[Route("api/long-tips")]
[Authorize]
public class LongTipsController(CurrentUserService currentUser, ILongTermTipsService longTips) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var snapshot = await longTips.GetForUserAsync(user.Id, ct);
        if (snapshot is null)
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "No tournament configured",
                detail: "Tournament must be seeded before long-term tips can be read.");
        }
        return Ok(ToResponse(snapshot));
    }

    [HttpPut]
    public async Task<IActionResult> Upsert([FromBody] LongTipsRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var cmd = new LongTermTipUpsertCommand(user.Id, request.WinnerTeamId, request.TopScorerPlayerId);
        var result = await longTips.UpsertAsync(cmd, ct);

        return result switch
        {
            LongTermTipUpsertResult.Success s => Ok(ToResponse(s.Snapshot)),

            LongTermTipUpsertResult.TournamentNotConfigured => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "No tournament configured",
                detail: "Tournament must be seeded before long-term tips can be submitted."),

            LongTermTipUpsertResult.TeamNotFound t => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Team not found",
                detail: $"No team with id {t.TeamId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TeamNotFound" }),

            LongTermTipUpsertResult.PlayerNotFound p => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Player not found",
                detail: $"No player with id {p.PlayerId}.",
                extensions: new Dictionary<string, object?> { ["reason"] = "TopScorerPlayerNotFound" }),

            LongTermTipUpsertResult.Rejected r => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: ReasonTitle(r.Validation.Reason),
                detail: r.Validation.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["reason"] = r.Validation.Reason.ToString(),
                }),

            _ => throw new InvalidOperationException($"Unhandled result: {result.GetType().Name}"),
        };
    }

    private static LongTipsResponse ToResponse(LongTermTipSnapshot s) => new(
        s.WinnerTeamId, s.WinnerTeamName,
        s.TopScorerPlayerId, s.TopScorerPlayerName, s.TopScorerTeamCode,
        s.WinnerSubmittedAt, s.TopScorerSubmittedAt,
        s.LockUtc, s.Locked);

    private static string ReasonTitle(LongTermTipRejectionReason reason) => reason switch
    {
        LongTermTipRejectionReason.Locked => "Long-term tips locked",
        LongTermTipRejectionReason.NothingProvided => "No tips provided",
        _ => "Long-term tip rejected",
    };
}
