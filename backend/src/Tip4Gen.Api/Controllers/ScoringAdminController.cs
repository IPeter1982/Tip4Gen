using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tip4Gen.Api.Auth;
using Tip4Gen.Infrastructure.Scoring;

namespace Tip4Gen.Api.Controllers;

public record RescoreResponse(Guid MatchId, int TipsScored, int TotalPoints);

[ApiController]
[Route("api/admin/matches")]
[Authorize(Policy = AuthExtensions.AdminPolicy)]
public class ScoringAdminController(IMatchScoringService scoring) : ControllerBase
{
    [HttpPost("{matchId:guid}/rescore")]
    public async Task<IActionResult> Rescore(Guid matchId, CancellationToken ct)
    {
        var result = await scoring.ScoreMatchAsync(matchId, ct);
        return result switch
        {
            MatchScoringResult.Success s => Ok(new RescoreResponse(s.MatchId, s.TipsScored, s.TotalPoints)),

            MatchScoringResult.MatchNotFound => Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Match not found",
                detail: $"No match with id {matchId}."),

            MatchScoringResult.NotScorable ns => Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Match not scorable",
                detail: $"Match is in status {ns.Status}; only Finished/Awarded matches with a recorded score can be scored.",
                extensions: new Dictionary<string, object?>
                {
                    ["status"] = ns.Status.ToString(),
                }),

            _ => throw new InvalidOperationException($"Unhandled MatchScoringResult: {result.GetType().Name}"),
        };
    }
}
