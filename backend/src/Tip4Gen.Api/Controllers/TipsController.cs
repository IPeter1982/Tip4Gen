using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Domain.Tipping;
using Tip4Gen.Infrastructure.Tipping;

namespace Tip4Gen.Api.Controllers;

public record TipRequest(int HomeGoals, int AwayGoals, bool Joker);

public record TipResponse(
    Guid Id,
    Guid MatchId,
    int HomeGoals,
    int AwayGoals,
    bool Joker,
    DateTimeOffset SubmittedAt,
    DateTimeOffset UpdatedAt,
    DateTimeOffset DeadlineUtc);

[ApiController]
[Route("api/tips")]
[Authorize]
public class TipsController(CurrentUserService currentUser, ITipsService tips) : ControllerBase
{
    [HttpPut("{matchId:guid}")]
    public async Task<IActionResult> Upsert(Guid matchId, [FromBody] TipRequest request, CancellationToken ct)
    {
        var user = await currentUser.GetOrCreateAsync(ct);
        var cmd = new TipUpsertCommand(user.Id, matchId, request.HomeGoals, request.AwayGoals, request.Joker);
        var result = await tips.UpsertAsync(cmd, ct);

        return result switch
        {
            TipUpsertResult.Success s => StatusCode(
                s.Created ? StatusCodes.Status201Created : StatusCodes.Status200OK,
                new TipResponse(
                    s.Tip.Id, s.Tip.MatchId, s.Tip.HomeGoals, s.Tip.AwayGoals, s.Tip.Joker,
                    s.Tip.SubmittedAt, s.Tip.UpdatedAt, s.DeadlineUtc)),

            TipUpsertResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}."),

            TipUpsertResult.Rejected r => Problem(
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: ReasonTitle(r.Validation.Reason),
                detail: r.Validation.Message,
                extensions: new Dictionary<string, object?>
                {
                    ["reason"] = r.Validation.Reason.ToString(),
                }),

            _ => throw new InvalidOperationException($"Unhandled TipUpsertResult: {result.GetType().Name}"),
        };
    }

    private static string ReasonTitle(TipRejectionReason reason) => reason switch
    {
        TipRejectionReason.DeadlinePassed => "Tipping closed",
        TipRejectionReason.ScoreOutOfRange => "Invalid score",
        TipRejectionReason.JokerNotAllowedOnKnockoutMatch => "Joker not allowed",
        TipRejectionReason.JokerQuotaExceeded => "Joker quota exceeded",
        _ => "Tip rejected",
    };
}
