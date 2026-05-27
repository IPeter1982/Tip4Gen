using Microsoft.Extensions.Logging;
using Tip4Gen.Domain.Tournaments.Events;

namespace Tip4Gen.Infrastructure.Scoring;

/// <summary>
/// Bridges MatchFinalized → MatchScoringService. The fixture sync service catches and
/// logs exceptions per-handler, so we let scoring errors propagate.
/// </summary>
public class MatchFinalizedScoringHandler(
    IMatchScoringService scoring,
    ILogger<MatchFinalizedScoringHandler> logger) : IMatchFinalizedHandler
{
    public async Task HandleAsync(MatchFinalized @event, CancellationToken ct)
    {
        var result = await scoring.ScoreMatchAsync(@event.MatchId, ct);
        switch (result)
        {
            case MatchScoringResult.Success s:
                logger.LogInformation(
                    "Scoring triggered by MatchFinalized for {MatchId}: {Tips} tips, {Total} points",
                    s.MatchId, s.TipsScored, s.TotalPoints);
                break;
            case MatchScoringResult.MatchNotFound:
                logger.LogWarning("MatchFinalized fired for {MatchId} but the match is missing", @event.MatchId);
                break;
            case MatchScoringResult.NotScorable ns:
                logger.LogWarning(
                    "MatchFinalized fired for {MatchId} but match is not scorable (status={Status})",
                    ns.MatchId, ns.Status);
                break;
        }
    }
}
